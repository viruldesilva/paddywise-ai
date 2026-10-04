using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Backend.Tests.PestDisease.Helpers;

/// <summary>Fresh InMemory ApplicationDbContext per test, same pattern the existing test
/// files already use (a Guid-named database so tests never share state).</summary>
public static class TestDbFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PestDiseaseTestDb_" + Guid.NewGuid())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>Two Pest and two Disease entries — enough to prove category filtering without
    /// being an arbitrary single-entry knowledge base.</summary>
    public static async Task SeedKnowledgeBaseAsync(ApplicationDbContext context)
    {
        context.PestDiseaseKnowledgeEntries.AddRange(
            new PestDiseaseKnowledge
            {
                Name = "Brown Planthopper",
                Category = PestDiseaseCategory.Pest,
                Symptoms = "Hopper burn, yellowing and drying of leaves.",
                ManagementGuidance = "Drain field water; apply recommended insecticide.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Name = "Yellow Stem Borer",
                Category = PestDiseaseCategory.Pest,
                Symptoms = "Dead hearts in vegetative stage, white heads at reproductive stage.",
                ManagementGuidance = "Remove and destroy affected tillers.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Name = "Rice Blast",
                Category = PestDiseaseCategory.Disease,
                Symptoms = "Diamond-shaped lesions with gray centers on leaves.",
                ManagementGuidance = "Apply recommended fungicide; avoid excess nitrogen.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Name = "Bacterial Leaf Blight",
                Category = PestDiseaseCategory.Disease,
                Symptoms = "Water-soaked lesions turning yellow to white along leaf margins.",
                ManagementGuidance = "Use resistant varieties; avoid excess nitrogen.",
                Source = "Sri Lanka Department of Agriculture"
            });

        await context.SaveChangesAsync();
    }

    /// <summary>Seeds one farmer, their field and an active cultivation cycle on it —
    /// the minimum graph ObservationService/ObservationsController navigate through
    /// (CropObservation -> CultivationCycle -> Field -> Farmer).</summary>
    public static async Task<CultivationCycle> SeedFarmerCycleAsync(
        ApplicationDbContext context, int farmerId, string farmerName)
    {
        var division = new Division { Name = "Test Division", District = "Test District", Province = "Test Province" };
        context.Divisions.Add(division);

        var farmer = new User
        {
            Id = farmerId,
            Name = farmerName,
            Email = $"{farmerName.ToLowerInvariant().Replace(" ", ".")}@example.com",
            PasswordHash = "unused-in-tests",
            Role = UserRole.Farmer
        };
        context.Users.Add(farmer);

        var field = new Field
        {
            Name = $"{farmerName}'s Field",
            Area = 1.5m,
            SoilType = "Clay",
            IrrigationType = "Canal",
            FarmerId = farmerId,
            Division = division
        };
        context.Fields.Add(field);

        var cycle = new CultivationCycle
        {
            Field = field,
            VarietyId = 1,
            Season = Season.Maha,
            Year = 2026,
            Method = CultivationMethod.Transplanting,
            SowingDate = new DateOnly(2026, 9, 1),
            ExpectedHarvestDate = new DateOnly(2026, 12, 1),
            CurrentStage = GrowthStage.Tillering,
            Status = CycleStatus.Active
        };
        context.CultivationCycles.Add(cycle);

        await context.SaveChangesAsync();
        return cycle;
    }

    /// <summary>Seeds one CropObservation reported by <paramref name="reportedByUserId"/> on
    /// the given cycle.</summary>
    public static async Task<CropObservation> SeedObservationAsync(
        ApplicationDbContext context,
        CultivationCycle cycle,
        int reportedByUserId,
        ObservationType observationType = ObservationType.Disease,
        string symptoms = "Yellowing leaf tips and stunted growth.")
    {
        var observation = new CropObservation
        {
            CultivationCycle = cycle,
            CultivationCycleId = cycle.Id,
            ReportedByUserId = reportedByUserId,
            ObservationType = observationType,
            CropStage = cycle.CurrentStage,
            Symptoms = symptoms,
            Severity = ObservationSeverity.Moderate
        };
        context.CropObservations.Add(observation);
        await context.SaveChangesAsync();
        return observation;
    }
}
