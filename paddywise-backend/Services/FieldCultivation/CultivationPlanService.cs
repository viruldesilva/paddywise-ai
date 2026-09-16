using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    private readonly ILogger<CultivationPlanService> _logger;

    public CultivationPlanService(
        ApplicationDbContext context,
        IAgent<PlanAgentInput, CultivationPlanOutput> agent,
        ILogger<CultivationPlanService> logger)
    {
        _context = context;
        _agent = agent;
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

            // TODO(next task): run the deterministic plan validator here. When it reports
            // failures, write them to ValidationErrorsJson and set Status = ValidationFailed
            // instead of PendingOfficerApproval.
            plan.Status = PlanStatus.PendingOfficerApproval;
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
