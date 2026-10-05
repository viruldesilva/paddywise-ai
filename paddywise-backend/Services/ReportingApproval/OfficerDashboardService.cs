using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.ReportingApproval;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.ReportingApproval;

public class OfficerDashboardService : IOfficerDashboardService
{
    private readonly ApplicationDbContext _context;

    public OfficerDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OfficerDashboardDto?> GetOfficerDashboardAsync(int officerId, CancellationToken ct = default)
    {
        var officer = await _context.Users
            .AsNoTracking()
            .Include(u => u.Division)
            .FirstOrDefaultAsync(u => u.Id == officerId && u.Role == UserRole.AgriculturalOfficer, ct);

        if (officer == null)
            return null;

        // Default to Division 1 (Medirigiriya) if unassigned
        var divisionId = officer.DivisionId ?? 1;
        var division = officer.Division ?? await _context.Divisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == divisionId, ct);

        var divisionName = division?.Name ?? "Medirigiriya";
        var district = division?.District ?? "Polonnaruwa";
        var province = division?.Province ?? "North Central";

        // Determine cultivation season: active cycle in division wins, otherwise Sri Lankan agronomic calendar
        var now = DateTime.UtcNow;
        var activeCycle = await _context.CultivationCycles
            .AsNoTracking()
            .Where(c => c.Field.DivisionId == divisionId && c.Status == CycleStatus.Active)
            .Select(c => new { c.Season, c.Year })
            .FirstOrDefaultAsync(ct);

        var currentSeason = activeCycle?.Season ?? ((now.Month >= 4 && now.Month <= 9) ? Season.Yala : Season.Maha);
        var currentYear = activeCycle?.Year ?? now.Year;
        var seasonLabel = $"{currentSeason} {currentYear}";

        // Approved treatments this season in this division
        var approvedTreatmentsCount = await _context.PestDiseaseReports
            .AsNoTracking()
            .Where(r => r.Status == PestDiseaseReportStatus.Approved &&
                        r.CropObservation.CultivationCycle.Field.DivisionId == divisionId &&
                        r.CropObservation.CultivationCycle.Season == currentSeason &&
                        r.CropObservation.CultivationCycle.Year == currentYear)
            .CountAsync(ct);

        // Distinct registered farmers with active fields in this division
        var registeredFarmerCount = await _context.Fields
            .AsNoTracking()
            .Where(f => f.DivisionId == divisionId && f.IsActive)
            .Select(f => f.FarmerId)
            .Distinct()
            .CountAsync(ct);

        // Pending treatment queue for this division
        var pendingRaw = await _context.PestDiseaseReports
            .AsNoTracking()
            .Where(r => r.Status == PestDiseaseReportStatus.PendingOfficerReview &&
                        r.CropObservation.CultivationCycle.Field.DivisionId == divisionId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                FarmerName = r.CropObservation.ReportedByUser.Name,
                DivisionName = r.CropObservation.CultivationCycle.Field.Division.Name,
                Symptoms = r.CropObservation.Symptoms,
                PossibleIssue = r.PossibleIssue,
                Confidence = r.Confidence,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        var issueNames = pendingRaw.Select(p => p.PossibleIssue).Distinct().ToList();
        var guidanceDict = await _context.PestDiseaseKnowledgeEntries
            .AsNoTracking()
            .Where(k => issueNames.Contains(k.Name))
            .ToDictionaryAsync(k => k.Name, k => k.ManagementGuidance, ct);

        var pendingQueue = pendingRaw.Select(p =>
        {
            var guidance = guidanceDict.TryGetValue(p.PossibleIssue, out var g) && !string.IsNullOrWhiteSpace(g)
                ? g
                : "Drain field water and apply Department of Agriculture approved treatment at recommended dosage.";

            return new PendingTreatmentPlanDto
            {
                Id = p.Id,
                FarmerName = string.IsNullOrWhiteSpace(p.FarmerName) ? "Unknown Farmer" : p.FarmerName,
                GnDivision = p.DivisionName,
                Symptom = p.Symptoms,
                AiDiagnosis = p.PossibleIssue,
                Confidence = p.Confidence,
                ProposedPlan = guidance,
                CreatedAt = p.CreatedAt
            };
        }).ToList();

        return new OfficerDashboardDto
        {
            AssignedDivision = new AssignedDivisionDto
            {
                Id = divisionId,
                Name = divisionName,
                Centre = "Agrarian Services Centre",
                District = district,
                Province = province
            },
            PendingApprovalCount = pendingQueue.Count,
            ApprovedTreatmentsThisSeason = approvedTreatmentsCount,
            CultivationSeason = seasonLabel,
            RegisteredFarmerCount = registeredFarmerCount,
            GnDivisionCount = null, // No GN division entity in schema
            PendingQueue = pendingQueue
        };
    }
}
