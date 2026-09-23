using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.PestDisease;

public class ObservationService : IObservationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly IAgent<DelegatedTask, DelegatedTaskResult> _diagnosisAgent;
    private readonly ILogger<ObservationService> _logger;

    public ObservationService(
        ApplicationDbContext context,
        [FromKeyedServices(AgentNames.PestDiseaseDiagnosis)] IAgent<DelegatedTask, DelegatedTaskResult> diagnosisAgent,
        ILogger<ObservationService> logger)
    {
        _context = context;
        _diagnosisAgent = diagnosisAgent;
        _logger = logger;
    }

    public async Task<List<ObservationResponseDto>> GetObservationsAsync(
        int callerId, UserRole callerRole, int? cultivationCycleId, int? fieldId)
    {
        var query = _context.CropObservations
            .AsNoTracking()
            .Include(o => o.CultivationCycle)
                .ThenInclude(c => c.Field)
            .Include(o => o.ReportedByUser)
            .Include(o => o.Reports)
            .AsQueryable();

        if (callerRole == UserRole.Farmer)
            query = query.Where(o => o.ReportedByUserId == callerId);

        if (cultivationCycleId.HasValue)
            query = query.Where(o => o.CultivationCycleId == cultivationCycleId.Value);

        if (fieldId.HasValue)
            query = query.Where(o => o.CultivationCycle.FieldId == fieldId.Value);

        var observations = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .ToListAsync();

        return observations.Select(MapToResponse).ToList();
    }

    public async Task<ObservationResponseDto?> GetByIdAsync(int observationId, int callerId, UserRole callerRole)
    {
        var observation = await _context.CropObservations
            .AsNoTracking()
            .Include(o => o.CultivationCycle)
                .ThenInclude(c => c.Field)
            .Include(o => o.ReportedByUser)
            .Include(o => o.Reports)
            .FirstOrDefaultAsync(o => o.Id == observationId);

        if (observation == null)
            return null;

        // Officers and admins may read any observation; a farmer only reads their own.
        if (callerRole == UserRole.Farmer && observation.ReportedByUserId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this observation.");

        return MapToResponse(observation);
    }

    public async Task<ObservationResponseDto> CreateAsync(int farmerId, CreateObservationRequestDto request)
    {
        var observationType = ParseEnum<ObservationType>(
            request.ObservationType, "Observation type must be Pest, Disease or Unknown.");
        var severity = ParseEnum<ObservationSeverity>(
            request.Severity, "Severity must be Low, Moderate or Severe.");

        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .FirstOrDefaultAsync(c => c.Id == request.CultivationCycleId);

        if (cycle == null)
            throw new InvalidOperationException("Cultivation cycle not found.");

        if (cycle.Field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        var observation = new CropObservation
        {
            CultivationCycleId = cycle.Id,
            ReportedByUserId = farmerId,
            ObservationType = observationType,
            // Snapshot of the cycle's stage right now — it may move on before this is reviewed.
            CropStage = cycle.CurrentStage,
            Symptoms = request.Symptoms,
            Severity = severity,
            ImageUrl = request.ImageUrl
        };

        _context.CropObservations.Add(observation);
        await _context.SaveChangesAsync();

        await _context.Entry(observation).Reference(o => o.ReportedByUser).LoadAsync();
        observation.CultivationCycle = cycle;

        return MapToResponse(observation);
    }

    public async Task<ObservationResponseDto?> UpdateAsync(
        int observationId, int farmerId, UpdateObservationRequestDto request)
    {
        var observationType = ParseEnum<ObservationType>(
            request.ObservationType, "Observation type must be Pest, Disease or Unknown.");
        var severity = ParseEnum<ObservationSeverity>(
            request.Severity, "Severity must be Low, Moderate or Severe.");

        var observation = await _context.CropObservations
            .Include(o => o.CultivationCycle)
                .ThenInclude(c => c.Field)
            .Include(o => o.ReportedByUser)
            .Include(o => o.Reports)
            .FirstOrDefaultAsync(o => o.Id == observationId);

        if (observation == null)
            return null;

        if (observation.ReportedByUserId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this observation.");

        // Once the agent has produced a diagnosis, the report it analyzed cannot shift
        // under it — a corrected report needs a fresh observation and analysis run.
        if (observation.Reports.Count > 0)
            throw new InvalidOperationException(
                "This observation already has a diagnosis and can no longer be edited.");

        observation.ObservationType = observationType;
        observation.Symptoms = request.Symptoms;
        observation.Severity = severity;
        observation.ImageUrl = request.ImageUrl;
        observation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(observation);
    }

    public async Task<ObservationResponseDto?> RequestAnalysisAsync(int observationId, int farmerId)
    {
        var observation = await _context.CropObservations
            .Include(o => o.CultivationCycle)
                .ThenInclude(c => c.Field)
            .Include(o => o.ReportedByUser)
            .Include(o => o.Reports)
            .FirstOrDefaultAsync(o => o.Id == observationId);

        if (observation == null)
            return null;

        if (observation.ReportedByUserId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this observation.");

        if (observation.Reports.Count > 0)
            throw new InvalidOperationException("This observation already has a diagnosis.");

        var correlationId = Guid.NewGuid().ToString("N");
        var agentInput = new CropAnalysisAgentInput
        {
            ObservationId = observation.Id,
            CultivationId = observation.CultivationCycleId,
            CropStage = observation.CropStage.ToString(),
            Symptoms = observation.Symptoms,
            Severity = observation.Severity.ToString(),
            ImageUrl = observation.ImageUrl
        };
        var task = new DelegatedTask
        {
            TaskType = "DiagnoseObservation",
            CultivationCycleId = observation.CultivationCycleId,
            PayloadJson = JsonSerializer.Serialize(agentInput, JsonOptions)
        };
        var agentContext = new AgentContext { RequestedByUserId = farmerId, CorrelationId = correlationId };

        var stopwatch = Stopwatch.StartNew();
        AgentResult<DelegatedTaskResult> dispatch;

        try
        {
            dispatch = await _diagnosisAgent.RunAsync(task, agentContext, CancellationToken.None);
        }
        catch (LlmException ex)
        {
            // The provider being down or rate-limiting is a failed run, not a crashed request.
            _logger.LogWarning(
                ex,
                "Diagnosis agent for observation {ObservationId} (correlation {CorrelationId}) failed with status {StatusCode}.",
                observationId,
                correlationId,
                (int)ex.StatusCode);

            dispatch = new AgentResult<DelegatedTaskResult>
            {
                Success = false,
                Error = $"The diagnosis assistant is unavailable right now ({(int)ex.StatusCode}). Please try again.",
                Duration = stopwatch.Elapsed
            };
        }

        stopwatch.Stop();

        CropAnalysisAgentOutput? agentOutput = null;
        var success = dispatch.Success;
        var error = dispatch.Success ? null : dispatch.Error;
        var rawOutput = dispatch.Output?.ResultJson ?? dispatch.Error ?? string.Empty;

        if (success)
        {
            try
            {
                agentOutput = string.IsNullOrWhiteSpace(dispatch.Output?.ResultJson)
                    ? null
                    : JsonSerializer.Deserialize<CropAnalysisAgentOutput>(dispatch.Output!.ResultJson!, JsonOptions);
            }
            catch (JsonException)
            {
                agentOutput = null;
            }

            // An empty PossibleIssues list is a legitimate "no likely match found" outcome per
            // the agent's own contract, not a failure — only a parse failure (null) is.
            if (agentOutput == null)
            {
                success = false;
                error = "The diagnosis agent did not return a valid result. Please try again shortly.";
            }
        }

        var duration = dispatch.Duration > TimeSpan.Zero ? dispatch.Duration : stopwatch.Elapsed;

        _context.DiagnosisRunLogs.Add(new DiagnosisRunLog
        {
            CropObservationId = observation.Id,
            AgentName = _diagnosisAgent.Name,
            CorrelationId = correlationId,
            InputJson = JsonSerializer.Serialize(task, JsonOptions),
            ToolCallsJson = JsonSerializer.Serialize(dispatch.ToolCalls, JsonOptions),
            RawOutput = rawOutput,
            Success = success,
            Error = error,
            DurationMs = (int)duration.TotalMilliseconds
        });

        if (!success)
        {
            await _context.SaveChangesAsync();
            throw new InvalidOperationException(error ?? "The diagnosis agent run failed.");
        }

        foreach (var candidate in agentOutput!.PossibleIssues)
        {
            observation.Reports.Add(new PestDiseaseReport
            {
                CropObservationId = observation.Id,
                PossibleIssue = candidate.Name,
                Confidence = candidate.Confidence,
                Status = PestDiseaseReportStatus.PendingOfficerReview
            });
        }

        await _context.SaveChangesAsync();

        return MapToResponse(observation);
    }

    private static ObservationResponseDto MapToResponse(CropObservation observation) => new()
    {
        Id = observation.Id,
        CultivationCycleId = observation.CultivationCycleId,
        FieldId = observation.CultivationCycle?.FieldId ?? 0,
        FieldName = observation.CultivationCycle?.Field?.Name ?? string.Empty,
        ReportedByUserId = observation.ReportedByUserId,
        ReportedByUserName = observation.ReportedByUser?.Name ?? string.Empty,
        ObservationType = observation.ObservationType.ToString(),
        CropStage = observation.CropStage.ToString(),
        Symptoms = observation.Symptoms,
        Severity = observation.Severity.ToString(),
        ImageUrl = observation.ImageUrl,
        CreatedAt = observation.CreatedAt,
        UpdatedAt = observation.UpdatedAt,
        Reports = observation.Reports
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new PestDiseaseReportSummaryDto
            {
                Id = r.Id,
                PossibleIssue = r.PossibleIssue,
                Confidence = r.Confidence,
                Status = r.Status.ToString(),
                OfficerComment = r.OfficerComment,
                ReviewedAt = r.ReviewedAt,
                CreatedAt = r.CreatedAt
            })
            .ToList()
    };

    private static T ParseEnum<T>(string value, string message) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidOperationException(message);

        return parsed;
    }
}
