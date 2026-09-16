using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public class CycleService : ICycleService
{
    // How far a farmer may back-date or forward-plan a sowing.
    private const int MaxBackdateDays = 30;
    private const int MaxLookaheadDays = 365;

    private readonly ApplicationDbContext _context;

    public CycleService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// "Start Cultivation Cycle" — the component's business operation. Validates the
    /// field, the variety and the calendar, then plans the season from the sowing date.
    /// Returns null when the field does not exist; business-rule failures throw.
    /// </summary>
    public async Task<CycleResponseDto?> StartCycleAsync(int farmerId, CreateCycleRequestDto request)
    {
        var season = ParseEnum<Season>(request.Season, "Season must be either Yala or Maha.");
        var method = ParseEnum<CultivationMethod>(
            request.Method, "Method must be Broadcasting, Transplanting or DirectSeeding.");

        var field = await _context.Fields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId);

        // A soft-deleted field is gone as far as the API is concerned, so both cases are a 404.
        if (field == null || !field.IsActive)
            return null;

        if (field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this field.");

        var hasOpenCycle = await _context.CultivationCycles
            .AnyAsync(c => c.FieldId == field.Id &&
                          (c.Status == CycleStatus.Planned || c.Status == CycleStatus.Active));

        if (hasOpenCycle)
            throw new InvalidOperationException("Field already has an active cultivation cycle");

        var variety = await _context.Varieties
            .FirstOrDefaultAsync(v => v.Id == request.VarietyId);

        if (variety == null)
            throw new InvalidOperationException("Variety not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.SowingDate < today.AddDays(-MaxBackdateDays))
            throw new InvalidOperationException(
                $"Sowing date cannot be more than {MaxBackdateDays} days in the past.");

        if (request.SowingDate > today.AddDays(MaxLookaheadDays))
            throw new InvalidOperationException(
                $"Sowing date cannot be more than {MaxLookaheadDays} days in the future.");

        var duplicate = await _context.CultivationCycles
            .AnyAsync(c => c.FieldId == field.Id && c.Season == season && c.Year == request.Year);

        if (duplicate)
            throw new InvalidOperationException(
                $"This field already has a {season} {request.Year} cultivation cycle.");

        var cycle = new CultivationCycle
        {
            FieldId = field.Id,
            VarietyId = variety.Id,
            Season = season,
            Year = request.Year,
            Method = method,
            SowingDate = request.SowingDate,
            ExpectedHarvestDate = request.SowingDate.AddDays(variety.DurationDays),
            CurrentStage = GrowthStage.Nursery,
            Status = request.SowingDate > today ? CycleStatus.Planned : CycleStatus.Active,
            Notes = request.Notes
        };

        _context.CultivationCycles.Add(cycle);
        await _context.SaveChangesAsync();

        await _context.Entry(cycle).Reference(c => c.Field).LoadAsync();
        await _context.Entry(cycle).Reference(c => c.Variety).LoadAsync();

        return MapToResponse(cycle, new List<GrowthStageLog>());
    }

    public async Task<List<CycleResponseDto>> GetMyCyclesAsync(int farmerId, int? fieldId = null)
    {
        var query = _context.CultivationCycles
            .AsNoTracking()
            .Include(c => c.Field)
            .Include(c => c.Variety)
            .Where(c => c.Field.FarmerId == farmerId);

        if (fieldId.HasValue)
            query = query.Where(c => c.FieldId == fieldId.Value);

        var cycles = await query
            .OrderByDescending(c => c.SowingDate)
            .ToListAsync();

        if (cycles.Count == 0)
            return new List<CycleResponseDto>();

        var cycleIds = cycles.Select(c => c.Id).ToList();
        var logs = await LoadLogsAsync(cycleIds);

        return cycles
            .Select(c => MapToResponse(c, logs.TryGetValue(c.Id, out var l) ? l : new List<GrowthStageLog>()))
            .ToList();
    }

    public async Task<CycleResponseDto?> GetByIdAsync(int cycleId, int callerId, UserRole callerRole)
    {
        var cycle = await _context.CultivationCycles
            .AsNoTracking()
            .Include(c => c.Field)
            .Include(c => c.Variety)
            .FirstOrDefaultAsync(c => c.Id == cycleId);

        if (cycle == null)
            return null;

        // Officers and admins may read any cycle; a farmer only reads their own.
        if (callerRole == UserRole.Farmer && cycle.Field.FarmerId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        var logs = await LoadLogsAsync(new List<int> { cycle.Id });

        return MapToResponse(cycle, logs.TryGetValue(cycle.Id, out var l) ? l : new List<GrowthStageLog>());
    }

    public async Task<CycleResponseDto?> LogStageAsync(
        int cycleId, int callerId, UserRole callerRole, LogStageRequestDto request)
    {
        var stage = ParseEnum<GrowthStage>(
            request.Stage,
            "Stage must be one of Nursery, Tillering, PanicleInitiation, Flowering, GrainFilling or Harvest.");

        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .Include(c => c.Variety)
            .FirstOrDefaultAsync(c => c.Id == cycleId);

        if (cycle == null)
            return null;

        // The owning farmer or any officer may record an observation.
        if (callerRole == UserRole.Farmer && cycle.Field.FarmerId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        _context.GrowthStageLogs.Add(new GrowthStageLog
        {
            CultivationCycleId = cycle.Id,
            Stage = stage,
            ObservedOn = request.ObservedOn,
            Notes = request.Notes,
            LoggedByUserId = callerId
        });

        cycle.CurrentStage = stage;

        if (stage == GrowthStage.Harvest)
        {
            cycle.Status = CycleStatus.Harvested;
            cycle.ActualHarvestDate = request.ObservedOn;
        }

        cycle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var logs = await LoadLogsAsync(new List<int> { cycle.Id });

        return MapToResponse(cycle, logs.TryGetValue(cycle.Id, out var l) ? l : new List<GrowthStageLog>());
    }

    public async Task<CycleResponseDto?> UpdateStatusAsync(int cycleId, int farmerId, CycleStatus status)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .Include(c => c.Variety)
            .FirstOrDefaultAsync(c => c.Id == cycleId);

        if (cycle == null)
            return null;

        if (cycle.Field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        cycle.Status = status;
        cycle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var logs = await LoadLogsAsync(new List<int> { cycle.Id });

        return MapToResponse(cycle, logs.TryGetValue(cycle.Id, out var l) ? l : new List<GrowthStageLog>());
    }

    public async Task<List<VarietyResponseDto>> GetVarietiesAsync()
    {
        return await _context.Varieties
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VarietyResponseDto
            {
                Id = v.Id,
                Name = v.Name,
                DurationDays = v.DurationDays,
                AgeGroup = v.AgeGroup,
                Notes = v.Notes
            })
            .ToListAsync();
    }

    /// <summary>
    /// Stage logs are not a navigation collection on the cycle, so they are loaded
    /// for all the cycles being mapped in one query.
    /// </summary>
    private async Task<Dictionary<int, List<GrowthStageLog>>> LoadLogsAsync(List<int> cycleIds)
    {
        var logs = await _context.GrowthStageLogs
            .AsNoTracking()
            .Include(g => g.LoggedByUser)
            .Where(g => cycleIds.Contains(g.CultivationCycleId))
            .OrderBy(g => g.ObservedOn)
            .ThenBy(g => g.Id)
            .ToListAsync();

        return logs
            .GroupBy(g => g.CultivationCycleId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static T ParseEnum<T>(string value, string message) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidOperationException(message);

        return parsed;
    }

    private static CycleResponseDto MapToResponse(CultivationCycle cycle, List<GrowthStageLog> logs)
    {
        // The stored dates are the single source of truth for an existing cycle: a later
        // edit to the variety's duration must not move a plan that is already running.
        var durationDays = cycle.ExpectedHarvestDate.DayNumber - cycle.SowingDate.DayNumber;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new CycleResponseDto
        {
            Id = cycle.Id,
            FieldId = cycle.FieldId,
            FieldName = cycle.Field?.Name ?? string.Empty,
            VarietyId = cycle.VarietyId,
            VarietyName = cycle.Variety?.Name ?? string.Empty,
            DurationDays = durationDays,
            Season = cycle.Season.ToString(),
            Year = cycle.Year,
            Method = cycle.Method.ToString(),
            SowingDate = cycle.SowingDate,
            ExpectedHarvestDate = cycle.ExpectedHarvestDate,
            ActualHarvestDate = cycle.ActualHarvestDate,
            CurrentStage = cycle.CurrentStage.ToString(),
            ExpectedStageToday =
                StageTimelineCalculator.ExpectedStageOn(today, cycle.SowingDate, durationDays).ToString(),
            Status = cycle.Status.ToString(),
            Notes = cycle.Notes,
            CreatedAt = cycle.CreatedAt,
            UpdatedAt = cycle.UpdatedAt,
            Timeline = StageTimelineCalculator.Build(cycle.SowingDate, durationDays)
                .Select(w => new StageWindowDto
                {
                    Stage = w.Stage.ToString(),
                    Start = w.Start,
                    End = w.End
                })
                .ToList(),
            StageLogs = logs.Select(l => new StageLogDto
            {
                Id = l.Id,
                Stage = l.Stage.ToString(),
                ObservedOn = l.ObservedOn,
                Notes = l.Notes,
                LoggedByUserId = l.LoggedByUserId,
                LoggedByUserName = l.LoggedByUser?.Name ?? string.Empty,
                CreatedAt = l.CreatedAt
            }).ToList()
        };
    }
}
