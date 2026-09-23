using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;

namespace PaddyWise.Api.Agents.CropResource;

public class CropActivityTools
{
    private readonly ApplicationDbContext _context;

    public CropActivityTools(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CycleActivityBundle?> GetCycleActivityBundleAsync(int cycleId, CancellationToken ct = default)
    {
        var cycle = await _context.CultivationCycles
            .Include(c => c.Field)
                .ThenInclude(f => f!.Division)
            .Include(c => c.Variety)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct);

        if (cycle == null)
            return null;

        var activities = await _context.CropActivities
            .Where(a => a.CultivationCycleId == cycleId)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var das = Math.Max(0, today.DayNumber - cycle.SowingDate.DayNumber);

        var varietyDuration = cycle.Variety?.DurationDays ?? (cycle.ExpectedHarvestDate.DayNumber - cycle.SowingDate.DayNumber);
        if (varietyDuration <= 0) varietyDuration = 105; // default 3.5 month

        var stage = CalculateCurrentStage(das, varietyDuration);

        var irrigations = new List<ParsedIrrigation>();
        var fertilizers = new List<ParsedFertilizer>();
        var pesticides = new List<ParsedPesticide>();
        var others = new List<ParsedOther>();

        foreach (var act in activities)
        {
            try
            {
                using var doc = JsonDocument.Parse(act.DetailsJson);
                var root = doc.RootElement;

                switch (act.ActivityType)
                {
                    case CropActivityType.Irrigation:
                        irrigations.Add(new ParsedIrrigation
                        {
                            ActivityId = act.Id,
                            Date = act.Date,
                            WaterLevelCm = TryGetDouble(root, "waterLevel"),
                            DurationHours = TryGetDouble(root, "duration") ?? 0,
                            Source = TryGetString(root, "source") ?? "Canal"
                        });
                        break;

                    case CropActivityType.Fertilizer:
                        fertilizers.Add(new ParsedFertilizer
                        {
                            ActivityId = act.Id,
                            Date = act.Date,
                            Type = TryGetString(root, "type") ?? "Urea",
                            QuantityKgPerHa = TryGetDouble(root, "quantity") ?? 0,
                            CropStage = TryGetString(root, "cropStage") ?? stage,
                            Region = TryGetString(root, "region") ?? "Dry",
                            Method = TryGetString(root, "method") ?? "Broadcasting"
                        });
                        break;

                    case CropActivityType.Pesticide:
                        pesticides.Add(new ParsedPesticide
                        {
                            ActivityId = act.Id,
                            Date = act.Date,
                            Product = TryGetString(root, "product") ?? "Unknown Product",
                            TargetPest = TryGetString(root, "targetPest") ?? "General Pest",
                            Quantity = TryGetDouble(root, "quantity") ?? 0,
                            Method = TryGetString(root, "method") ?? "Spraying"
                        });
                        break;

                    case CropActivityType.Other:
                        others.Add(new ParsedOther
                        {
                            ActivityId = act.Id,
                            Date = act.Date,
                            SpecificActivity = TryGetString(root, "specificActivity") ?? "General",
                            Notes = TryGetString(root, "notes") ?? string.Empty
                        });
                        break;
                }
            }
            catch
            {
                // Ignore malformed JSON activity record
            }
        }

        return new CycleActivityBundle
        {
            Cycle = cycle,
            Today = today,
            DaysAfterSowing = das,
            EstimatedStage = stage,
            VarietyDurationDays = varietyDuration,
            Irrigations = irrigations,
            Fertilizers = fertilizers,
            Pesticides = pesticides,
            Others = others
        };
    }

    public static string CalculateCurrentStage(int das, int durationDays)
    {
        // Sri Lankan Rice Research & Development Institute standard stage windows
        var vegetativeEnd = (int)(durationDays * 0.45); // ~40-50 days
        var panicleEnd = (int)(durationDays * 0.65);    // ~60-70 days
        var floweringEnd = (int)(durationDays * 0.80);  // ~80-85 days

        if (das <= 14) return "Nursery / Establishment";
        if (das <= vegetativeEnd) return "Tillering";
        if (das <= panicleEnd) return "Panicle Initiation";
        if (das <= floweringEnd) return "Flowering";
        if (das < durationDays - 10) return "Grain Filling / Ripening";
        if (das <= durationDays) return "Harvest Ready";
        return "Harvested";
    }

    private static double? TryGetDouble(JsonElement element, string propName)
    {
        if (!element.TryGetProperty(propName, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var val)) return val;
        if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var sVal)) return sVal;
        return null;
    }

    private static string? TryGetString(JsonElement element, string propName)
    {
        if (!element.TryGetProperty(propName, out var prop)) return null;
        return prop.GetString();
    }
}

public class CycleActivityBundle
{
    public CultivationCycle Cycle { get; set; } = null!;
    public DateOnly Today { get; set; }
    public int DaysAfterSowing { get; set; }
    public string EstimatedStage { get; set; } = string.Empty;
    public int VarietyDurationDays { get; set; }

    public List<ParsedIrrigation> Irrigations { get; set; } = new();
    public List<ParsedFertilizer> Fertilizers { get; set; } = new();
    public List<ParsedPesticide> Pesticides { get; set; } = new();
    public List<ParsedOther> Others { get; set; } = new();
}

public class ParsedIrrigation
{
    public int ActivityId { get; set; }
    public DateOnly Date { get; set; }
    public double? WaterLevelCm { get; set; }
    public double DurationHours { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ParsedFertilizer
{
    public int ActivityId { get; set; }
    public DateOnly Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public double QuantityKgPerHa { get; set; }
    public string CropStage { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

public class ParsedPesticide
{
    public int ActivityId { get; set; }
    public DateOnly Date { get; set; }
    public string Product { get; set; } = string.Empty;
    public string TargetPest { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class ParsedOther
{
    public int ActivityId { get; set; }
    public DateOnly Date { get; set; }
    public string SpecificActivity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
