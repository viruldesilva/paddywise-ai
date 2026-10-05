using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Controllers.ReportingApproval;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class OfficerDashboardTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PaddyWiseDashboardTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetOfficerDashboardAsync_ReturnsScopedMetricsAndQueue()
    {
        using var context = CreateInMemoryDbContext();

        var division = new Division { Id = 1, Name = "Medirigiriya", District = "Polonnaruwa", Province = "North Central" };
        var otherDivision = new Division { Id = 2, Name = "Nuwaragam Palatha", District = "Anuradhapura", Province = "North Central" };
        context.Divisions.AddRange(division, otherDivision);

        var officer = new User { Id = 10, Name = "Officer Kamal", Email = "kamal@ag.lk", Role = UserRole.AgriculturalOfficer, DivisionId = 1 };
        var farmer1 = new User { Id = 20, Name = "Bandara Wanninayake", Email = "bandara@farmer.lk", Role = UserRole.Farmer };
        var farmer2 = new User { Id = 21, Name = "Sunil Perera", Email = "sunil@farmer.lk", Role = UserRole.Farmer };
        context.Users.AddRange(officer, farmer1, farmer2);

        var variety = new Variety { Id = 1, Name = "Bg 352", DurationDays = 105, AgeGroup = "3.5 month" };
        context.Varieties.Add(variety);

        var field1 = new Field { Id = 101, Name = "Track 5 Field", Area = 2.5m, FarmerId = 20, DivisionId = 1, IsActive = true };
        var field2 = new Field { Id = 102, Name = "Track 6 Field", Area = 1.5m, FarmerId = 20, DivisionId = 1, IsActive = true };
        var otherField = new Field { Id = 103, Name = "Other Division Field", Area = 3.0m, FarmerId = 21, DivisionId = 2, IsActive = true };
        context.Fields.AddRange(field1, field2, otherField);

        var cycle1 = new CultivationCycle
        {
            Id = 201,
            FieldId = 101,
            VarietyId = 1,
            Season = Season.Yala,
            Year = DateTime.UtcNow.Year,
            Status = CycleStatus.Active,
            SowingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            ExpectedHarvestDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(75))
        };
        context.CultivationCycles.Add(cycle1);

        var observation1 = new CropObservation
        {
            Id = 301,
            CultivationCycleId = 201,
            ReportedByUserId = 20,
            Symptoms = "Hopper burn patches in 0.5 acre paddy patch.",
            ObservationType = ObservationType.Pest,
            CropStage = GrowthStage.Tillering,
            Severity = ObservationSeverity.Severe
        };
        context.CropObservations.Add(observation1);

        var report1 = new PestDiseaseReport
        {
            Id = 401,
            CropObservationId = 301,
            PossibleIssue = "Brown Planthopper",
            Confidence = 0.96m,
            Status = PestDiseaseReportStatus.PendingOfficerReview,
            CreatedAt = DateTime.UtcNow
        };
        var report2 = new PestDiseaseReport
        {
            Id = 402,
            CropObservationId = 301,
            PossibleIssue = "Brown Planthopper",
            Confidence = 0.92m,
            Status = PestDiseaseReportStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        context.PestDiseaseReports.AddRange(report1, report2);

        var knowledge = new PestDiseaseKnowledge
        {
            Id = 1,
            Name = "Brown Planthopper",
            ManagementGuidance = "Drain field water for 3 days; apply approved Thiamethoxam 25% WG at recommended dosage.",
            Source = "DOA"
        };
        context.PestDiseaseKnowledgeEntries.Add(knowledge);

        await context.SaveChangesAsync();

        var service = new OfficerDashboardService(context);
        var result = await service.GetOfficerDashboardAsync(10);

        Assert.NotNull(result);
        Assert.Equal("Medirigiriya", result.AssignedDivision.Name);
        Assert.Equal("Agrarian Services Centre", result.AssignedDivision.Centre);
        Assert.Equal(1, result.RegisteredFarmerCount); // Only farmer1 has fields in division 1
        Assert.Equal(1, result.PendingApprovalCount);
        Assert.Single(result.PendingQueue);
        Assert.Equal(result.PendingApprovalCount, result.PendingQueue.Count);
        Assert.Equal("Bandara Wanninayake", result.PendingQueue[0].FarmerName);
        Assert.Equal("Hopper burn patches in 0.5 acre paddy patch.", result.PendingQueue[0].Symptom);
        Assert.Equal("Brown Planthopper", result.PendingQueue[0].AiDiagnosis);
        Assert.Equal(0.96m, result.PendingQueue[0].Confidence);
        Assert.Contains("Drain field water", result.PendingQueue[0].ProposedPlan);
        Assert.Equal(1, result.ApprovedTreatmentsThisSeason);
    }

    [Fact]
    public async Task OfficerDashboardController_ReturnsOk_WithDashboardDto()
    {
        using var context = CreateInMemoryDbContext();

        var division = new Division { Id = 1, Name = "Medirigiriya", District = "Polonnaruwa", Province = "North Central" };
        var officer = new User { Id = 99, Name = "Officer test", Email = "test@ag.lk", Role = UserRole.AgriculturalOfficer, DivisionId = 1 };
        context.Divisions.Add(division);
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var service = new OfficerDashboardService(context);
        var controller = new OfficerDashboardController(service);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, "AgriculturalOfficer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var response = await controller.GetDashboard(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(response);
        var dto = Assert.IsType<PaddyWise.Api.DTOs.ReportingApproval.OfficerDashboardDto>(okResult.Value);

        Assert.Equal("Medirigiriya", dto.AssignedDivision.Name);
        Assert.Equal(0, dto.PendingApprovalCount);
        Assert.Empty(dto.PendingQueue);
    }
}
