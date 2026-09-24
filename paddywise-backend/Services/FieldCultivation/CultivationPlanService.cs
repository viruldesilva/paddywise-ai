using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public class CultivationPlanService : ICultivationPlanService
{
    private readonly ApplicationDbContext _context;
    private readonly IAgent<PlanAgentInput, CultivationPlanOutput> _agent;

    // The delegate agents are registered under a DI key, so they are resolved by name at
    // dispatch time rather than injected — the plan decides which of them it needs.
    private readonly IServiceProvider _services;
    private readonly ILogger<CultivationPlanService> _logger;

    public CultivationPlanService(
        ApplicationDbContext context,
        IAgent<PlanAgentInput, CultivationPlanOutput> agent,
        IServiceProvider services,
        ILogger<CultivationPlanService> logger)
    {
        _context = context;
        _agent = agent;
        _services = services;
        _logger = logger;
    }

    public async Task<CultivationPlanResponseDto?> RequestPlanAsync(int farmerId, int cycleId, string objective)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .FirstOrDefaultAsync(c => c.Id == cycleId);

        if (cycle == null)
            return null;

        if (cycle.Field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        // A plan already with an officer, or already signed off, is not replaced behind their back.
        var blocking = await _context.CultivationPlans
            .AnyAsync(p => p.CultivationCycleId == cycleId &&
                          (p.Status == PlanStatus.PendingOfficerApproval || p.Status == PlanStatus.Approved));

        if (blocking)
            throw new InvalidOperationException(
                "This cycle already has a plan awaiting officer approval or already approved.");

        var plan = new CultivationPlan
        {
            CultivationCycleId = cycleId,
            RequestedByUserId = farmerId,
            Objective = objective,
            Status = PlanStatus.Draft
        };

        // Saved before the agent runs so the run log can point at the plan whatever happens.
        _context.CultivationPlans.Add(plan);
        await _context.SaveChangesAsync();

        var correlationId = Guid.NewGuid().ToString("N");
        var input = new PlanAgentInput { CycleId = cycleId, Objective = objective };
        var context = new AgentContext { RequestedByUserId = farmerId, CorrelationId = correlationId };

        var stopwatch = Stopwatch.StartNew();
        AgentResult<CultivationPlanOutput> result;

        try
        {
            result = await _agent.RunAsync(input, context, CancellationToken.None);
        }
        catch (LlmException ex)
        {
            // The provider being down or rate-limiting is a failed run, not a crashed request.
            _logger.LogWarning(
                ex,
                "Planning agent for cycle {CycleId} (correlation {CorrelationId}) failed with status {StatusCode}.",
                cycleId,
                correlationId,
                (int)ex.StatusCode);

            stopwatch.Stop();

            result = new AgentResult<CultivationPlanOutput>
            {
                Success = false,
                Error = $"The planning assistant is unavailable right now ({(int)ex.StatusCode}). Please try again.",
                Duration = stopwatch.Elapsed
            };
        }

        stopwatch.Stop();

        var rawOutput = result.Success && result.Output != null
            // The agent hands back a parsed plan, so the plan itself is what gets kept verbatim.
            ? JsonSerializer.Serialize(result.Output, CultivationPlanJson.Options)
            : result.Error ?? string.Empty;

        var duration = result.Duration > TimeSpan.Zero ? result.Duration : stopwatch.Elapsed;

        _context.AgentRunLogs.Add(new AgentRunLog
        {
            CultivationPlanId = plan.Id,
            AgentName = _agent.Name,
            CorrelationId = correlationId,
            InputJson = JsonSerializer.Serialize(input, CultivationPlanJson.Options),
            ToolCallsJson = JsonSerializer.Serialize(result.ToolCalls, CultivationPlanJson.Options),
            RawOutput = rawOutput,
            Success = result.Success,
            Error = result.Success ? null : result.Error,
            DurationMs = (int)duration.TotalMilliseconds
        });

        if (result.Success && result.Output != null)
        {
            plan.PlanJson = rawOutput;

            var validation = Validate(result.Output, cycle);

            if (validation.IsValid)
            {
                plan.Status = PlanStatus.PendingOfficerApproval;
                plan.ValidationErrorsJson = null;

                // Only a plan that passed the gate hands work to the other components.
                await DispatchDelegationsAsync(plan, result.Output, context);
            }
            else
            {
                plan.Status = PlanStatus.ValidationFailed;
                plan.ValidationErrorsJson = JsonSerializer.Serialize(validation.Errors);
            }
        }
        else
        {
            plan.Status = PlanStatus.ValidationFailed;
            plan.ValidationErrorsJson = JsonSerializer.Serialize(
                new[] { result.Error ?? "The planning agent did not return a usable plan." });
        }

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await MapToResponseAsync(plan);
    }

    /// <summary>
    /// Runs the deterministic validator against the cycle's own stored dates — the variety's
    /// current duration must not move the timeline a running cycle was planned against.
    /// </summary>
    private static ValidationResult Validate(CultivationPlanOutput output, CultivationCycle cycle)
    {
        var durationDays = cycle.ExpectedHarvestDate.DayNumber - cycle.SowingDate.DayNumber;

        if (durationDays < 1)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>
                {
                    "This cycle's expected harvest date is not after its sowing date, " +
                    "so the plan cannot be checked against a stage timeline."
                }
            };
        }

        return CultivationPlanValidator.Validate(
            output,
            StageTimelineCalculator.Build(cycle.SowingDate, durationDays),
            cycle.SowingDate,
            cycle.ExpectedHarvestDate,
            DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Hands every delegation in the plan to the agent it names, one dispatch at a time. Each
    /// gets its own AgentRunLog row under the same correlation id, and a delegate agent that
    /// fails is recorded as a failed run without disturbing the plan's own status — the plan
    /// itself passed validation, so it still belongs in the officer's queue.
    /// </summary>
    private async Task DispatchDelegationsAsync(
        CultivationPlan plan,
        CultivationPlanOutput output,
        AgentContext context)
    {
        foreach (var delegation in output.Delegations)
        {
            // PayloadJson stays opaque: the receiving component owns the shape it expects.
            var task = new DelegatedTask
            {
                TaskType = delegation.Instruction,
                CultivationCycleId = plan.CultivationCycleId,
                CultivationPlanId = plan.Id,
                PayloadJson = JsonSerializer.Serialize(delegation.Payload, CultivationPlanJson.Options)
            };

            var stopwatch = Stopwatch.StartNew();
            var toolCalls = new List<ToolCallRecord>();
            bool success;
            string? error;
            var rawOutput = string.Empty;

            var agent = _services.GetKeyedService<IAgent<DelegatedTask, DelegatedTaskResult>>(
                delegation.TargetAgent);

            if (agent == null)
            {
                // Validation rules this out, so reaching here means a missing registration.
                success = false;
                error = $"No agent is registered under '{delegation.TargetAgent}'.";
                _logger.LogWarning(
                    "Plan {PlanId} delegates to {TargetAgent}, which is not registered.",
                    plan.Id,
                    delegation.TargetAgent);
            }
            else
            {
                try
                {
                    var dispatch = await agent.RunAsync(task, context, CancellationToken.None);

                    success = dispatch.Success;
                    error = dispatch.Success ? null : dispatch.Error;
                    toolCalls = dispatch.ToolCalls;
                    rawOutput = dispatch.Output == null
                        ? error ?? string.Empty
                        : JsonSerializer.Serialize(dispatch.Output, CultivationPlanJson.Options);
                }
                catch (Exception ex)
                {
                    // A delegate agent is another component's code; whatever it throws is its
                    // run failing, not this request failing.
                    _logger.LogWarning(
                        ex,
                        "Delegation from plan {PlanId} to {TargetAgent} (correlation {CorrelationId}) threw.",
                        plan.Id,
                        delegation.TargetAgent,
                        context.CorrelationId);

                    success = false;
                    error = ex.Message;
                }
            }

            stopwatch.Stop();

            _context.AgentRunLogs.Add(new AgentRunLog
            {
                CultivationPlanId = plan.Id,
                AgentName = delegation.TargetAgent,
                CorrelationId = context.CorrelationId,
                InputJson = JsonSerializer.Serialize(task, CultivationPlanJson.Options),
                ToolCallsJson = JsonSerializer.Serialize(toolCalls, CultivationPlanJson.Options),
                RawOutput = rawOutput,
                Success = success,
                Error = error,
                DurationMs = (int)stopwatch.Elapsed.TotalMilliseconds
            });
        }
    }

    public async Task<CultivationPlanResponseDto?> ReviewAsync(int planId, int officerId, ReviewPlanDto request)
    {
        var decision = ParseDecision(request.Decision);

        var plan = await _context.CultivationPlans
            .Include(p => p.CultivationCycle)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null)
            return null;

        if (plan.Status != PlanStatus.PendingOfficerApproval)
        {
            throw new InvalidOperationException(
                $"This plan is {plan.Status} — only a plan awaiting officer approval can be reviewed.");
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        if (decision != PlanReviewDecision.Approve && comment == null)
        {
            throw new InvalidOperationException(
                decision == PlanReviewDecision.Reject
                    ? "A comment is required when rejecting a plan."
                    : "A comment is required when asking for a revision.");
        }

        plan.Status = decision switch
        {
            PlanReviewDecision.Approve => PlanStatus.Approved,
            PlanReviewDecision.Reject => PlanStatus.Rejected,
            _ => PlanStatus.RevisionRequested
        };

        plan.OfficerId = officerId;
        plan.OfficerComment = comment;
        plan.ReviewedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        if (decision == PlanReviewDecision.Approve)
        {
            // Approving is what starts the season: the plan and the cycle move together or
            // not at all, so neither can be left describing a state the other contradicts.
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (plan.CultivationCycle.Status == CycleStatus.Planned)
                {
                    plan.CultivationCycle.Status = CycleStatus.Active;
                    plan.CultivationCycle.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        else
        {
            // Rejection and revision leave the cycle alone; the farmer may ask for a new plan.
            await _context.SaveChangesAsync();
        }

        return await MapToResponseAsync(plan);
    }

    public async Task<List<PendingPlanSummaryDto>> GetPendingAsync(int? divisionId)
    {
        // Projected, not Included: the queue needs four names, not four entity graphs.
        var query = _context.CultivationPlans
            .AsNoTracking()
            .Where(p => p.Status == PlanStatus.PendingOfficerApproval);

        if (divisionId != null)
            query = query.Where(p => p.CultivationCycle.Field.DivisionId == divisionId.Value);

        // Season stays an enum until the rows are in memory: the wire form is the
        // member name, and translating that into SQL buys nothing on a queue read.
        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new
            {
                PlanId = p.Id,
                CycleId = p.CultivationCycleId,
                FarmerName = p.CultivationCycle.Field.Farmer.Name,
                FieldName = p.CultivationCycle.Field.Name,
                DivisionName = p.CultivationCycle.Field.Division.Name,
                p.CultivationCycle.Season,
                p.CultivationCycle.Year,
                p.Objective,
                p.CreatedAt
            })
            .ToListAsync();

        return rows
            .Select(r => new PendingPlanSummaryDto
            {
                PlanId = r.PlanId,
                CycleId = r.CycleId,
                FarmerName = r.FarmerName,
                FieldName = r.FieldName,
                DivisionName = r.DivisionName,
                Season = r.Season.ToString(),
                Year = r.Year,
                Objective = r.Objective,
                CreatedAt = r.CreatedAt
            })
            .ToList();
    }

    private static PlanReviewDecision ParseDecision(string value)
    {
        if (!Enum.TryParse<PlanReviewDecision>(value, true, out var decision) || !Enum.IsDefined(decision))
            throw new InvalidOperationException("Decision must be Approve, Reject or RequestRevision.");

        return decision;
    }

    public async Task<CultivationPlanResponseDto?> GetByIdAsync(int planId, int callerId, UserRole callerRole)
    {
        var plan = await _context.CultivationPlans
            .AsNoTracking()
            .Include(p => p.CultivationCycle)
                .ThenInclude(c => c.Field)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null)
            return null;

        // Officers and admins may read any plan; a farmer only reads their own.
        if (callerRole == UserRole.Farmer && plan.CultivationCycle.Field.FarmerId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation plan.");

        return await MapToResponseAsync(plan);
    }

    public async Task<List<CultivationPlanResponseDto>?> GetForCycleAsync(
        int cycleId, int callerId, UserRole callerRole)
    {
        var cycle = await _context.CultivationCycles
            .AsNoTracking()
            .Include(c => c.Field)
            .FirstOrDefaultAsync(c => c.Id == cycleId);

        if (cycle == null)
            return null;

        if (callerRole == UserRole.Farmer && cycle.Field.FarmerId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        var plans = await _context.CultivationPlans
            .AsNoTracking()
            .Where(p => p.CultivationCycleId == cycleId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        if (plans.Count == 0)
            return new List<CultivationPlanResponseDto>();

        var planIds = plans.Select(p => p.Id).ToList();

        var runs = await _context.AgentRunLogs
            .AsNoTracking()
            .Where(l => l.CultivationPlanId != null && planIds.Contains(l.CultivationPlanId.Value))
            .OrderBy(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var runsByPlan = runs
            .GroupBy(l => l.CultivationPlanId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return plans
            .Select(p => MapToResponse(
                p,
                runsByPlan.TryGetValue(p.Id, out var planRuns) ? planRuns : new List<AgentRunLog>()))
            .ToList();
    }

    private async Task<CultivationPlanResponseDto> MapToResponseAsync(CultivationPlan plan)
    {
        var runs = await _context.AgentRunLogs
            .AsNoTracking()
            .Where(l => l.CultivationPlanId == plan.Id)
            .OrderBy(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .ToListAsync();

        return MapToResponse(plan, runs);
    }

    private static CultivationPlanResponseDto MapToResponse(CultivationPlan plan, List<AgentRunLog> runs) =>
        new()
        {
            Id = plan.Id,
            CycleId = plan.CultivationCycleId,
            Objective = plan.Objective,
            Status = plan.Status.ToString(),
            Plan = Deserialize<CultivationPlanOutput>(plan.PlanJson),
            ValidationErrors = Deserialize<List<string>>(plan.ValidationErrorsJson) ?? new List<string>(),
            OfficerComment = plan.OfficerComment,
            ReviewedAt = plan.ReviewedAt,
            CreatedAt = plan.CreatedAt,
            AgentRuns = runs.Select(l => new AgentRunSummaryDto
            {
                AgentName = l.AgentName,
                Success = l.Success,
                Error = l.Error,
                DurationMs = l.DurationMs,
                ToolCalls = Deserialize<List<ToolCallRecord>>(l.ToolCallsJson) ?? new List<ToolCallRecord>(),
                CreatedAt = l.CreatedAt
            }).ToList()
        };

    /// <summary>
    /// Reads a stored jsonb column. A row written by an older shape must not break the read,
    /// so an unparseable value comes back as null rather than throwing.
    /// </summary>
    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, CultivationPlanJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
