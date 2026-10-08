using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Backend.Tests.FieldCultivation.Helpers;

/// <summary>
/// Fresh InMemory ApplicationDbContext per test plus the Component 1 graph the services and
/// the agent navigate (Cycle -> Field -> Division/Farmer, Cycle -> Variety).
///
/// Differs from PestDisease's TestDbFactory in two ways it has to: it seeds a real Variety
/// (the agent's get_cycle Includes it), and it ignores InMemory's TransactionIgnoredWarning,
/// because CultivationPlanService.ReviewAsync opens a transaction when approving and the
/// InMemory provider otherwise turns that into an exception. Cycles are sown relative to
/// today, since the validator and the agent both read DateTime.UtcNow.
/// </summary>
public static class FcTestDb
{
    public const int DurationDays = 120;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static DbContextOptions<ApplicationDbContext> Options(string dbName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public static ApplicationDbContext CreateContext(string? dbName = null) =>
        new(Options(dbName ?? "FieldCultivationTestDb_" + Guid.NewGuid()));

    public static async Task<Division> SeedDivisionAsync(
        ApplicationDbContext context, string name = "Test Division")
    {
        var division = new Division { Name = name, District = "Test District", Province = "Test Province" };
        context.Divisions.Add(division);
        await context.SaveChangesAsync();
        return division;
    }

    public static async Task<Variety> SeedVarietyAsync(
        ApplicationDbContext context, int durationDays = DurationDays, string name = "Bg 360")
    {
        var variety = new Variety { Name = name, DurationDays = durationDays, AgeGroup = "4 month" };
        context.Varieties.Add(variety);
        await context.SaveChangesAsync();
        return variety;
    }

    public static async Task<User> SeedUserAsync(
        ApplicationDbContext context, int id, string name, UserRole role = UserRole.Farmer)
    {
        var user = new User
        {
            Id = id,
            Name = name,
            Email = $"{name.ToLowerInvariant().Replace(" ", ".")}@example.com",
            PasswordHash = "unused-in-tests",
            Role = role
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<Field> SeedFieldAsync(
        ApplicationDbContext context, int farmerId, Division division, string name = "North Paddy")
    {
        var field = new Field
        {
            Name = name,
            Area = 1.5m,
            SoilType = "Clay",
            IrrigationType = "Canal",
            FarmerId = farmerId,
            DivisionId = division.Id
        };
        context.Fields.Add(field);
        await context.SaveChangesAsync();
        return field;
    }

    public static async Task<CultivationCycle> SeedCycleAsync(
        ApplicationDbContext context,
        Field field,
        Variety variety,
        int sownDaysAgo = 20,
        CycleStatus status = CycleStatus.Active,
        Season season = Season.Maha,
        int? year = null)
    {
        var sowing = Today.AddDays(-sownDaysAgo);
        var cycle = new CultivationCycle
        {
            FieldId = field.Id,
            VarietyId = variety.Id,
            Season = season,
            Year = year ?? sowing.Year,
            Method = CultivationMethod.Transplanting,
            SowingDate = sowing,
            ExpectedHarvestDate = sowing.AddDays(variety.DurationDays),
            CurrentStage = GrowthStage.Tillering,
            Status = status
        };
        context.CultivationCycles.Add(cycle);
        await context.SaveChangesAsync();
        return cycle;
    }

    /// <summary>One farmer, their field and a cycle on it — the usual starting point.</summary>
    public static async Task<CultivationCycle> SeedFarmerCycleAsync(
        ApplicationDbContext context,
        int farmerId,
        string farmerName,
        int sownDaysAgo = 20,
        CycleStatus status = CycleStatus.Active,
        Division? division = null)
    {
        division ??= await SeedDivisionAsync(context);
        var variety = await SeedVarietyAsync(context);
        await SeedUserAsync(context, farmerId, farmerName);
        var field = await SeedFieldAsync(context, farmerId, division, $"{farmerName}'s Field");
        return await SeedCycleAsync(context, field, variety, sownDaysAgo, status);
    }

    public static async Task<CultivationPlan> SeedPlanAsync(
        ApplicationDbContext context,
        CultivationCycle cycle,
        int farmerId,
        PlanStatus status,
        DateTime? createdAt = null,
        string objective = "Plan the rest of the season.")
    {
        var plan = new CultivationPlan
        {
            CultivationCycleId = cycle.Id,
            RequestedByUserId = farmerId,
            Objective = objective,
            Status = status,
            PlanJson = "{}",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();
        return plan;
    }
}
