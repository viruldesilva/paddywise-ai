using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.CropResource;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.CropResource;

public class CropActivityService : ICropActivityService
{
    private readonly ApplicationDbContext _context;

    public CropActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CropActivityDto>> GetActivitiesForCycleAsync(int cycleId, int userId, string userRole, CancellationToken cancellationToken = default)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
                .ThenInclude(f => f!.Farmer)
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken);

        if (cycle == null)
            throw new InvalidOperationException("Cultivation cycle not found.");

        if (userRole == "Farmer" && cycle.Field!.FarmerId != userId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        var activities = await _context.CropActivities
            .Include(a => a.LoggedByUser)
            .Include(a => a.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .Where(a => a.CultivationCycleId == cycleId)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return activities.Select(a => new CropActivityDto
        {
            Id = a.Id,
            CultivationCycleId = a.CultivationCycleId,
            ActivityType = a.ActivityType.ToString(),
            Date = a.Date,
            DetailsJson = a.DetailsJson,
            LoggedByUserId = a.LoggedByUserId,
            LoggedByUserName = a.LoggedByUser?.Name ?? "Unknown",
            CreatedAt = a.CreatedAt,
            FieldName = a.CultivationCycle?.Field?.Name ?? cycle.Field?.Name,
            FarmerName = a.CultivationCycle?.Field?.Farmer?.Name ?? cycle.Field?.Farmer?.Name ?? a.LoggedByUser?.Name,
            FarmerId = a.CultivationCycle?.Field?.FarmerId ?? cycle.Field?.FarmerId,
            CycleName = $"{cycle.Season} {cycle.Year}"
        }).ToList();
    }

    public async Task<List<CropActivityDto>> GetAllActivitiesAsync(int? farmerId = null, int? cycleId = null, string? activityType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CropActivities
            .AsNoTracking()
            .Include(a => a.LoggedByUser)
            .Include(a => a.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .AsQueryable();

        if (farmerId.HasValue)
        {
            query = query.Where(a => a.CultivationCycle != null && a.CultivationCycle.Field != null && a.CultivationCycle.Field.FarmerId == farmerId.Value);
        }

        if (cycleId.HasValue)
        {
            query = query.Where(a => a.CultivationCycleId == cycleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(activityType) && Enum.TryParse<CropActivityType>(activityType, true, out var typeEnum))
        {
            query = query.Where(a => a.ActivityType == typeEnum);
        }

        var activities = await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return activities.Select(a => new CropActivityDto
        {
            Id = a.Id,
            CultivationCycleId = a.CultivationCycleId,
            ActivityType = a.ActivityType.ToString(),
            Date = a.Date,
            DetailsJson = a.DetailsJson,
            LoggedByUserId = a.LoggedByUserId,
            LoggedByUserName = a.LoggedByUser?.Name ?? "Unknown",
            CreatedAt = a.CreatedAt,
            FieldName = a.CultivationCycle?.Field?.Name ?? "Unknown Field",
            FarmerName = a.CultivationCycle?.Field?.Farmer?.Name ?? a.LoggedByUser?.Name ?? "Unknown Farmer",
            FarmerId = a.CultivationCycle?.Field?.FarmerId,
            CycleName = a.CultivationCycle != null ? $"{a.CultivationCycle.Season} {a.CultivationCycle.Year}" : null
        }).ToList();
    }

    public async Task<CropActivityDto> CreateActivityAsync(int cycleId, CreateCropActivityRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken);

        if (cycle == null)
            throw new InvalidOperationException("Cultivation cycle not found.");

        if (cycle.Field!.FarmerId != userId)
            throw new UnauthorizedAccessException("Only the farmer who owns the field can log activities for this cycle.");

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var activity = new CropActivity
        {
            CultivationCycleId = cycleId,
            ActivityType = request.ActivityType,
            Date = request.Date,
            DetailsJson = request.DetailsJson,
            LoggedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.CropActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CropActivityDto
        {
            Id = activity.Id,
            CultivationCycleId = activity.CultivationCycleId,
            ActivityType = activity.ActivityType.ToString(),
            Date = activity.Date,
            DetailsJson = activity.DetailsJson,
            LoggedByUserId = activity.LoggedByUserId,
            LoggedByUserName = user.Name,
            CreatedAt = activity.CreatedAt
        };
    }
}
