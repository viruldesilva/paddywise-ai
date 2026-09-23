using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.PestDisease;

public class PestDiseaseReportService : IPestDiseaseReportService
{
    private readonly ApplicationDbContext _context;

    public PestDiseaseReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PestDiseaseReportResponseDto>> GetReportsAsync(
        int callerId, UserRole callerRole, string? status, int? observationId, int? cultivationCycleId)
    {
        var query = _context.PestDiseaseReports
            .AsNoTracking()
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.CultivationCycle)
                    .ThenInclude(c => c.Field)
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.ReportedByUser)
            .Include(r => r.Officer)
            .AsQueryable();

        if (callerRole == UserRole.Farmer)
            query = query.Where(r => r.CropObservation.ReportedByUserId == callerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = ParseEnum<PestDiseaseReportStatus>(status, "Status must be PendingOfficerReview, Approved, Rejected or RevisionRequested.");
            query = query.Where(r => r.Status == parsedStatus);
        }

        if (observationId.HasValue)
            query = query.Where(r => r.CropObservationId == observationId.Value);

        if (cultivationCycleId.HasValue)
            query = query.Where(r => r.CropObservation.CultivationCycleId == cultivationCycleId.Value);

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync();

        return reports.Select(MapToResponse).ToList();
    }

    public async Task<PestDiseaseReportResponseDto?> GetByIdAsync(int reportId, int callerId, UserRole callerRole)
    {
        var report = await _context.PestDiseaseReports
            .AsNoTracking()
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.CultivationCycle)
                    .ThenInclude(c => c.Field)
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.ReportedByUser)
            .Include(r => r.Officer)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return null;

        // Officers and admins may read any report; a farmer only reads diagnoses on their own
        // observations.
        if (callerRole == UserRole.Farmer && report.CropObservation.ReportedByUserId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this report.");

        return MapToResponse(report);
    }

    public async Task<PestDiseaseReportResponseDto?> ReviewAsync(
        int reportId, int officerId, ReviewPestDiseaseReportDto request)
    {
        var decision = ParseDecision(request.Decision);

        var report = await _context.PestDiseaseReports
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.CultivationCycle)
                    .ThenInclude(c => c.Field)
            .Include(r => r.CropObservation)
                .ThenInclude(o => o.ReportedByUser)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return null;

        if (report.Status != PestDiseaseReportStatus.PendingOfficerReview)
        {
            throw new InvalidOperationException(
                $"This report is {report.Status} — only a report awaiting officer review can be reviewed.");
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        if (decision != PestDiseaseReportReviewDecision.Approve && comment == null)
        {
            throw new InvalidOperationException(
                decision == PestDiseaseReportReviewDecision.Reject
                    ? "A comment is required when rejecting a report."
                    : "A comment is required when asking for a revision.");
        }

        report.Status = decision switch
        {
            PestDiseaseReportReviewDecision.Approve => PestDiseaseReportStatus.Approved,
            PestDiseaseReportReviewDecision.Reject => PestDiseaseReportStatus.Rejected,
            _ => PestDiseaseReportStatus.RevisionRequested
        };

        report.OfficerId = officerId;
        report.OfficerComment = comment;
        report.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(report).Reference(r => r.Officer).LoadAsync();

        return MapToResponse(report);
    }

    private static PestDiseaseReportResponseDto MapToResponse(PestDiseaseReport report) => new()
    {
        Id = report.Id,
        CropObservationId = report.CropObservationId,
        CultivationCycleId = report.CropObservation.CultivationCycleId,
        FieldId = report.CropObservation.CultivationCycle?.FieldId ?? 0,
        FieldName = report.CropObservation.CultivationCycle?.Field?.Name ?? string.Empty,
        ReportedByUserId = report.CropObservation.ReportedByUserId,
        ReportedByUserName = report.CropObservation.ReportedByUser?.Name ?? string.Empty,
        ObservationType = report.CropObservation.ObservationType.ToString(),
        CropStage = report.CropObservation.CropStage.ToString(),
        Symptoms = report.CropObservation.Symptoms,
        Severity = report.CropObservation.Severity.ToString(),
        ImageUrl = report.CropObservation.ImageUrl,
        PossibleIssue = report.PossibleIssue,
        Confidence = report.Confidence,
        Status = report.Status.ToString(),
        OfficerId = report.OfficerId,
        OfficerName = report.Officer?.Name,
        OfficerComment = report.OfficerComment,
        ReviewedAt = report.ReviewedAt,
        CreatedAt = report.CreatedAt
    };

    private static T ParseEnum<T>(string value, string message) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidOperationException(message);

        return parsed;
    }

    private static PestDiseaseReportReviewDecision ParseDecision(string value)
    {
        if (!Enum.TryParse<PestDiseaseReportReviewDecision>(value, true, out var decision) || !Enum.IsDefined(decision))
            throw new InvalidOperationException("Decision must be Approve, Reject or RequestRevision.");

        return decision;
    }
}
