using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.ReportingApproval;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
public class DatabaseIntegrationTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "DatabaseIntegrationTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // 1. TC-DB-01: AccountStatus column default value is Approved
    [Fact]
    public async Task UserEntity_AccountStatusDefaultValue_IsApproved()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Name = "Default Status Farmer",
            Email = "defaultstatus@test.com",
            PasswordHash = "hash",
            Role = UserRole.Farmer
            // Note: AccountStatus not explicitly assigned here
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "defaultstatus@test.com");
        Assert.NotNull(savedUser);
        Assert.Equal(AccountStatus.Approved, savedUser.AccountStatus);

        // Also check model metadata configuration
        var accountStatusProperty = context.Model.FindEntityType(typeof(User))
            ?.FindProperty(nameof(User.AccountStatus));
        Assert.NotNull(accountStatusProperty);
        Assert.Equal(AccountStatus.Approved, accountStatusProperty.GetDefaultValue());
    }

    // 2. TC-DB-02: Notification entity FK and cascade behavior metadata
    [Fact]
    public void NotificationEntity_ConfiguresCascadeDeleteWithUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act: Verify the EF Core Model relationship metadata configured in OnModelCreating
        var entityType = context.Model.FindEntityType(typeof(Notification));
        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        // Assert
        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.Contains(nameof(Notification.UserId), foreignKey.Properties.Select(p => p.Name));
    }

    // 3. TC-DB-03: AgentRunLog & DiagnosisRunLog allow nullable FK for decoupled system actions
    [Fact]
    public async Task AuditLogs_AllowNullForeignKeys_AndSetNullOnEntityDeletion()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // 3a. AgentRunLog without plan (e.g. system check or before plan is created)
        var detachedAgentLog = new AgentRunLog
        {
            CultivationPlanId = null,
            AgentName = "SystemValidationAgent",
            CorrelationId = Guid.NewGuid().ToString(),
            Success = true,
            RawOutput = "System sanity check passed"
        };
        context.AgentRunLogs.Add(detachedAgentLog);

        // 3b. DiagnosisRunLog without observation
        var detachedDiagLog = new DiagnosisRunLog
        {
            CropObservationId = null,
            AgentName = "SystemDiagnosisAgent",
            CorrelationId = Guid.NewGuid().ToString(),
            Success = true,
            RawOutput = "System sanity check passed"
        };
        context.DiagnosisRunLogs.Add(detachedDiagLog);

        await context.SaveChangesAsync();

        // Assert persistence
        var savedAgentLog = await context.AgentRunLogs.FindAsync(detachedAgentLog.Id);
        Assert.NotNull(savedAgentLog);
        Assert.Null(savedAgentLog.CultivationPlanId);

        var savedDiagLog = await context.DiagnosisRunLogs.FindAsync(detachedDiagLog.Id);
        Assert.NotNull(savedDiagLog);
        Assert.Null(savedDiagLog.CropObservationId);

        // Verify DeleteBehavior is SetNull so deleting an entity never erases the audit log
        var agentLogFk = context.Model.FindEntityType(typeof(AgentRunLog))
            ?.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CultivationPlan));
        Assert.NotNull(agentLogFk);
        Assert.Equal(DeleteBehavior.SetNull, agentLogFk.DeleteBehavior);

        var diagLogFk = context.Model.FindEntityType(typeof(DiagnosisRunLog))
            ?.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(CropObservation));
        Assert.NotNull(diagLogFk);
        Assert.Equal(DeleteBehavior.SetNull, diagLogFk.DeleteBehavior);
    }

    // 4. TC-DB-04: Transaction Decoupling (Option A Orchestration)
    // If NotificationMessageService's Gemini call fails mid-flow, confirm the parent CultivationPlan status update is committed
    [Fact]
    public async Task NotificationFailure_DoesNotRollbackParentPlanStatusUpdate()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var plan = new CultivationPlan
        {
            Id = 505,
            CultivationCycleId = 1,
            RequestedByUserId = 42,
            Objective = "Test Transaction Decoupling",
            Status = PlanStatus.PendingOfficerApproval
        };
        context.CultivationPlans.Add(plan);
        await context.SaveChangesAsync();

        // Simulate Review Endpoint step 1: Officer approves the plan
        plan.Status = PlanStatus.Approved;
        plan.OfficerId = 99;
        plan.OfficerComment = "Approved by officer.";
        plan.ReviewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Simulate Review Endpoint step 2: Call NotificationMessageService with throwing Gemini LLM
        var mockLlm = new Mock<ILlmClient>();
        mockLlm
            .Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Gemini API connection timed out."));

        var notificationService = new NotificationMessageService(
            context,
            mockLlm.Object,
            NullLogger<NotificationMessageService>.Instance);

        // Act: Generate notification (Option A design: must never throw)
        var exception = await Record.ExceptionAsync(() =>
            notificationService.GenerateAndCreateCultivationPlanNotificationAsync(plan.Id));
        Assert.Null(exception);

        // Assert: Parent CultivationPlan status remains Approved in DB
        var planInDb = await context.CultivationPlans.FindAsync(505);
        Assert.NotNull(planInDb);
        Assert.Equal(PlanStatus.Approved, planInDb.Status);
        Assert.Equal(99, planInDb.OfficerId);

        // Assert: Fallback notification was still created for farmer despite Gemini failure
        var notificationInDb = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == 42);
        Assert.NotNull(notificationInDb);
        Assert.Contains("Approved", notificationInDb.Message);

        // Assert: Audit log recorded the failure without rolling back the transaction
        var auditLog = await context.AgentRunLogs.FirstOrDefaultAsync(l => l.CultivationPlanId == 505);
        Assert.NotNull(auditLog);
        Assert.False(auditLog.Success);
        Assert.Contains("timed out", auditLog.Error);
    }
}
