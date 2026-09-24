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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneWeekAgo = today.AddDays(-7);

        if (request.Date > today)
        {
            throw new ArgumentException("Activity date cannot be in the future.");
        }

        if (request.Date < oneWeekAgo)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be older than the past week ({oneWeekAgo:yyyy-MM-dd}). Activities can only be logged within the last 7 days.");
        }

        if (request.Date < cycle.SowingDate)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be earlier than the cultivation cycle's sowing date ({cycle.SowingDate:yyyy-MM-dd}).");
        }

        if (cycle.ActualHarvestDate.HasValue && request.Date > cycle.ActualHarvestDate.Value)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be after the harvest date ({cycle.ActualHarvestDate.Value:yyyy-MM-dd}).");
        }

        ValidateActivityDetails(request.ActivityType, request.DetailsJson);

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

    public async Task<CropActivityDto> UpdateActivityAsync(int activityId, UpdateCropActivityRequestDto request, int userId, string userRole, CancellationToken cancellationToken = default)
    {
        var activity = await _context.CropActivities
            .Include(a => a.LoggedByUser)
            .Include(a => a.CultivationCycle)
                .ThenInclude(c => c!.Field)
                    .ThenInclude(f => f!.Farmer)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity == null)
            throw new InvalidOperationException("Crop activity not found.");

        if (userRole == "Farmer" && activity.LoggedByUserId != userId && activity.CultivationCycle?.Field?.FarmerId != userId)
            throw new UnauthorizedAccessException("You do not have permission to edit this activity.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneWeekAgo = today.AddDays(-7);

        if (request.Date > today)
        {
            throw new ArgumentException("Activity date cannot be in the future.");
        }

        if (request.Date < oneWeekAgo)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be older than the past week ({oneWeekAgo:yyyy-MM-dd}). Activities can only be recorded/updated within the last 7 days.");
        }

        if (activity.CultivationCycle != null && request.Date < activity.CultivationCycle.SowingDate)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be earlier than the cultivation cycle's sowing date ({activity.CultivationCycle.SowingDate:yyyy-MM-dd}).");
        }

        if (activity.CultivationCycle != null && activity.CultivationCycle.ActualHarvestDate.HasValue && request.Date > activity.CultivationCycle.ActualHarvestDate.Value)
        {
            throw new ArgumentException($"Activity date ({request.Date:yyyy-MM-dd}) cannot be after the harvest date ({activity.CultivationCycle.ActualHarvestDate.Value:yyyy-MM-dd}).");
        }

        ValidateActivityDetails(request.ActivityType, request.DetailsJson);

        activity.ActivityType = request.ActivityType;
        activity.Date = request.Date;
        activity.DetailsJson = request.DetailsJson;

        await _context.SaveChangesAsync(cancellationToken);

        return new CropActivityDto
        {
            Id = activity.Id,
            CultivationCycleId = activity.CultivationCycleId,
            ActivityType = activity.ActivityType.ToString(),
            Date = activity.Date,
            DetailsJson = activity.DetailsJson,
            LoggedByUserId = activity.LoggedByUserId,
            LoggedByUserName = activity.LoggedByUser?.Name ?? "Unknown",
            CreatedAt = activity.CreatedAt,
            FieldName = activity.CultivationCycle?.Field?.Name ?? "Unknown Field",
            FarmerName = activity.CultivationCycle?.Field?.Farmer?.Name ?? activity.LoggedByUser?.Name ?? "Unknown Farmer",
            FarmerId = activity.CultivationCycle?.Field?.FarmerId,
            CycleName = activity.CultivationCycle != null ? $"{activity.CultivationCycle.Season} {activity.CultivationCycle.Year}" : null
        };
    }

    public async Task DeleteActivityAsync(int activityId, int userId, string userRole, CancellationToken cancellationToken = default)
    {
        var activity = await _context.CropActivities
            .Include(a => a.CultivationCycle)
                .ThenInclude(c => c!.Field)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity == null)
            throw new InvalidOperationException("Crop activity not found.");

        if (userRole == "Farmer" && activity.LoggedByUserId != userId && activity.CultivationCycle?.Field?.FarmerId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this activity.");

        _context.CropActivities.Remove(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateActivityDetails(CropActivityType activityType, string detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            throw new ArgumentException("Activity details cannot be empty.");
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(detailsJson);
            var root = doc.RootElement;

            switch (activityType)
            {
                case CropActivityType.Fertilizer:
                    if (!root.TryGetProperty("type", out var fType) || string.IsNullOrWhiteSpace(fType.GetString()))
                        throw new ArgumentException("Fertilizer type is required.");
                    if (!root.TryGetProperty("quantity", out var fQty) || !TryGetDouble(fQty, out var fQtyVal) || fQtyVal <= 0)
                        throw new ArgumentException("Fertilizer quantity must be a positive number greater than 0.");
                    if (!root.TryGetProperty("cropStage", out var fStage) || string.IsNullOrWhiteSpace(fStage.GetString()))
                        throw new ArgumentException("Crop stage is required.");
                    if (!root.TryGetProperty("region", out var fRegion) || string.IsNullOrWhiteSpace(fRegion.GetString()))
                        throw new ArgumentException("Climatic region/zone is required.");
                    if (!root.TryGetProperty("method", out var fMethod) || string.IsNullOrWhiteSpace(fMethod.GetString()))
                        throw new ArgumentException("Application method is required.");
                    break;

                case CropActivityType.Irrigation:
                    if (!root.TryGetProperty("waterLevel", out var wLvl) || !TryGetDouble(wLvl, out var wLvlVal) || wLvlVal < 0)
                        throw new ArgumentException("Water level must be a non-negative number.");
                    if (!root.TryGetProperty("duration", out var dur) || !TryGetDouble(dur, out var durVal) || durVal <= 0)
                        throw new ArgumentException("Duration must be a positive number greater than 0.");
                    if (!root.TryGetProperty("source", out var src) || string.IsNullOrWhiteSpace(src.GetString()))
                        throw new ArgumentException("Water source is required.");
                    break;

                case CropActivityType.Pesticide:
                    if (!root.TryGetProperty("product", out var prod) || string.IsNullOrWhiteSpace(prod.GetString()))
                        throw new ArgumentException("Product name is required.");
                    if (!root.TryGetProperty("targetPest", out var pest) || string.IsNullOrWhiteSpace(pest.GetString()))
                        throw new ArgumentException("Target pest/disease is required.");
                    if (!root.TryGetProperty("quantity", out var pQty) || !TryGetDouble(pQty, out var pQtyVal) || pQtyVal <= 0)
                        throw new ArgumentException("Pesticide quantity must be a positive number greater than 0.");
                    if (!root.TryGetProperty("method", out var pMethod) || string.IsNullOrWhiteSpace(pMethod.GetString()))
                        throw new ArgumentException("Application method is required.");
                    break;

                case CropActivityType.Other:
                    if (!root.TryGetProperty("specificActivity", out var spec) || string.IsNullOrWhiteSpace(spec.GetString()))
                        throw new ArgumentException("Specific activity type is required.");
                    break;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ArgumentException("Invalid JSON format for activity details.");
        }
    }

    private static bool TryGetDouble(System.Text.Json.JsonElement element, out double val)
    {
        val = 0;
        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
            return element.TryGetDouble(out val);
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            return double.TryParse(element.GetString(), out val);
        return false;
    }
}
