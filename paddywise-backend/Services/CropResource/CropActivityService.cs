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
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken);

        if (cycle == null)
            throw new InvalidOperationException("Cultivation cycle not found.");

        if (userRole == "Farmer" && cycle.Field!.FarmerId != userId)
            throw new UnauthorizedAccessException("You do not have access to this cultivation cycle.");

        var activities = await _context.CropActivities
            .Include(a => a.LoggedByUser)
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
            LoggedByUserName = a.LoggedByUser!.Name,
            CreatedAt = a.CreatedAt
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
