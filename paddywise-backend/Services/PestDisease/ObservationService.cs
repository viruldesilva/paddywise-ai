using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.PestDisease;

public class ObservationService : IObservationService
{
    private readonly ApplicationDbContext _context;

    public ObservationService(ApplicationDbContext context)
    {
        _context = context;
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
