using System;
using System.Collections.Generic;
using System.Linq;

namespace PaddyWise.Api.Agents.CropResource;

public class ActivitySafetyValidator
{
    // Sri Lanka Registrar of Pesticides (ROP) banned / severely restricted substances
    private static readonly HashSet<string> BannedOrRestrictedSubstances = new(StringComparer.OrdinalIgnoreCase)
    {
        "Carbofuran",
        "Chlorpyrifos",
        "Paraquat",
        "Monocrotophos",
        "Endosulfan",
        "Dimethoate",
        "Methamidophos",
        "Propanil",
        "Glyphosate",
        "DDT",
        "Aldrin"
    };

    public static SafetyAuditResult AuditActivitiesAndRecommendations(
        CycleActivityBundle bundle, 
        List<ActivityRecommendationDto> recommendations)
    {
        var result = new SafetyAuditResult();
        var today = bundle.Today;

        // 1. Check for banned / restricted pesticide use in history
        foreach (var pest in bundle.Pesticides)
        {
            var matchedBanned = BannedOrRestrictedSubstances.FirstOrDefault(b => 
                pest.Product.Contains(b, StringComparison.OrdinalIgnoreCase));

            if (matchedBanned != null)
            {
                result.HasCriticalHazard = true;
                result.RequiresOfficerReview = true;
                result.SafetyAlerts.Add(
                    $"CRITICAL REGULATORY ALERT: '{pest.Product}' recorded on {pest.Date:yyyy-MM-dd} contains {matchedBanned}, which is prohibited or strictly restricted for paddy in Sri Lanka under Registrar of Pesticides regulations. Do not handle without protective gear and inform your Agrarian Services Center immediately.");
            }
        }

        // 2. Pre-Harvest Interval (PHI) verification
        var daysToHarvest = bundle.Cycle.ExpectedHarvestDate.DayNumber - today.DayNumber;
        if (daysToHarvest <= 14 && daysToHarvest >= 0)
        {
            // Within 14 days of harvest: no pesticides or nitrogen
            var recentPesticide = bundle.Pesticides.Any(p => p.Date >= today.AddDays(-7));
            if (recentPesticide)
            {
                result.SafetyAlerts.Add(
                    "PRE-HARVEST INTERVAL (PHI) WARNING: Pesticide applied within 14 days of expected harvest date. Chemical residues may exceed national maximum residue limits (MRLs). Consult an Agricultural Officer before harvesting.");
                result.RequiresOfficerReview = true;
            }

            // In recommendations, strip any chemical pesticide or fertilizer recommendation
            recommendations.RemoveAll(r => r.Category == "Fertilizer" || r.Category == "Pest");
            recommendations.Add(new ActivityRecommendationDto
            {
                Category = "General",
                Priority = "HIGH",
                Action = "Do NOT apply any chemical pesticides or fertilizers during the final 14-day pre-harvest window.",
                Reason = "The crop is approaching harvest. All chemical applications must cease to comply with Pre-Harvest Intervals (PHI) and protect food safety.",
                Evidence = $"Expected harvest date is {bundle.Cycle.ExpectedHarvestDate:yyyy-MM-dd} ({daysToHarvest} days away).",
                ConfidenceScore = 0.98,
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "Office of the Registrar of Pesticides Sri Lanka", Section = "Pre-Harvest Interval (PHI) Statutory Standards" },
                    new CitationDto { Document = "Department of Agriculture Sri Lanka", Section = "Paddy Harvesting and Post-Harvest Safety Guidelines" }
                }
            });
        }

        // 3. Excessive Nitrogen & Split Check
        var recentUreas = bundle.Fertilizers
            .Where(f => f.Type.Contains("Urea", StringComparison.OrdinalIgnoreCase) && f.Date >= today.AddDays(-10))
            .ToList();

        var recentUreaQty = recentUreas.Sum(f => f.QuantityKgPerHa);

        if (recentUreaQty > 65)
        {
            result.SafetyAlerts.Add(
                $"NITROGEN EXCESS ALERT: A total of {recentUreaQty:F1} kg/ha Urea was applied in the past 10 days, exceeding the DOA safe single-split maximum of 65 kg/ha. Excessive Nitrogen increases susceptibility to Leaf Blast (Pyricularia oryzae) and Brown Planthopper (BPH) infestations.");
            
            // Override any recommendation asking to add fertilizer now
            recommendations.RemoveAll(r => r.Category == "Fertilizer" && r.Action.Contains("Apply", StringComparison.OrdinalIgnoreCase));
            recommendations.Insert(0, new ActivityRecommendationDto
            {
                Category = "Fertilizer",
                Priority = "HIGH",
                Action = "Suspend all Nitrogen / Urea applications for at least 14 days.",
                Reason = $"Total Urea applied in the past 10 days ({recentUreaQty:F1} kg/ha) exceeds the safe split limit. Additional nitrogen will lead to vegetative lodging, delayed maturity, and fungal outbreaks.",
                Evidence = $"Logged Urea: {string.Join(", ", recentUreas.Select(u => $"{u.QuantityKgPerHa} kg/ha on {u.Date:yyyy-MM-dd}"))}.",
                ConfidenceScore = 0.95,
                Citations = new List<CitationDto>
                {
                    new CitationDto { Document = "Department of Agriculture Sri Lanka - Rice Nutrient Guidelines", Section = "Split Application Limits and Toxicity Avoidance" }
                }
            });
        }

        // 4. Critical Water Stress at Flowering
        if (bundle.EstimatedStage == "Flowering")
        {
            var latestIrrigation = bundle.Irrigations.LastOrDefault();
            if (latestIrrigation == null || latestIrrigation.WaterLevelCm <= 1.0 || (today.DayNumber - latestIrrigation.Date.DayNumber) > 5)
            {
                result.SafetyAlerts.Add(
                    "CRITICAL MOISTURE STRESS ALERT: Your crop is in the Flowering stage. Moisture deficit at flowering causes spikelet sterility and irreversible grain yield loss.");
                
                recommendations.Insert(0, new ActivityRecommendationDto
                {
                    Category = "Irrigation",
                    Priority = "HIGH",
                    Action = "Irrigate immediately to maintain 3 - 5 cm standing water during Flowering.",
                    Reason = "Rice panicles during anthesis/flowering require continuous standing moisture. Any soil cracking or drought stress will cause empty grains (chaffy panicles).",
                    Evidence = latestIrrigation != null ? $"Last irrigation was {latestIrrigation.Date:yyyy-MM-dd} with {latestIrrigation.WaterLevelCm} cm water." : "No recent irrigation recorded.",
                    ConfidenceScore = 0.98,
                    Citations = new List<CitationDto>
                    {
                        new CitationDto { Document = "Rice Research and Development Institute (RRDI) Batalagoda", Section = "Water Management during Reproductive & Anthesis Phases" }
                    }
                });
            }
        }

        return result;
    }
}

public class SafetyAuditResult
{
    public bool HasCriticalHazard { get; set; }
    public bool RequiresOfficerReview { get; set; }
    public List<string> SafetyAlerts { get; set; } = new();
}
