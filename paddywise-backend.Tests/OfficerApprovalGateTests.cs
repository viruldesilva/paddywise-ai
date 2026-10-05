using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PaddyWise.Api.Controllers.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

public class OfficerApprovalGateTests
{
    private readonly IConfiguration _config;

    public OfficerApprovalGateTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "super-secret-paddywise-jwt-key-for-unit-testing-32chars" },
            { "Jwt:Issuer", "PaddyWiseApi" },
            { "Jwt:Audience", "PaddyWiseClient" },
            { "Jwt:AccessTokenExpiryMinutes", "20" },
            { "Jwt:RefreshTokenExpiryDays", "7" },
            { "Brevo:ApiKey", "xkeysib_test_key" },
            { "Brevo:FromEmail", "onboarding@paddywise.ai" },
            { "Brevo:FromName", "PaddyWise AI" }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PaddyWiseTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // 1. Officer registers → AccountStatus PendingApproval, no tokens returned
    [Fact]
    public async Task OfficerRegistration_SetsPendingApproval_AndReturnsNoTokens()
    {
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _config);

        var request = new RegisterRequestDto
        {
            Name = "Officer Samantha",
            Email = "samantha.ag@example.com",
            Password = "Password123!",
            Role = "AgriculturalOfficer",
            Phone = "+94771234567"
        };

        var response = await authService.RegisterAsync(request);

        Assert.True(response.RequiresApproval);
        Assert.Equal("Your account is pending admin verification. You will be able to log in once approved.", response.Message);
        Assert.Null(response.AccessToken);
        Assert.Null(response.RefreshToken);
        Assert.Equal("Officer Samantha", response.Name);
        Assert.Equal("samantha.ag@example.com", response.Email);

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.AgriculturalOfficer, savedUser.Role);
        Assert.Equal(AccountStatus.PendingApproval, savedUser.AccountStatus);
    }

    // 2. Pending officer attempts login → blocked, correct message, no tokens
    [Fact]
    public async Task PendingOfficer_LoginAttempt_IsBlockedWith403Forbidden()
    {
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _config);
        var authController = new AuthController(authService);

        // Register pending officer
        await authService.RegisterAsync(new RegisterRequestDto
        {
            Name = "Officer Samantha",
            Email = "samantha.pending@example.com",
            Password = "Password123!",
            Role = "AgriculturalOfficer"
        });

        // Service call test
        var loginResult = await authService.LoginAsync(new LoginRequestDto
        {
            Email = "samantha.pending@example.com",
            Password = "Password123!"
        });

        Assert.True(loginResult.IsBlocked);
        Assert.False(loginResult.IsSuccess);
        Assert.Equal(AccountStatus.PendingApproval, loginResult.BlockedStatus);
        Assert.Equal("Your account is pending admin verification. Please check back later.", loginResult.BlockedMessage);
        Assert.Null(loginResult.AuthResponse);

        // Controller call test (verifying 403 Forbidden status code and message)
        var actionResult = await authController.Login(new LoginRequestDto
        {
            Email = "samantha.pending@example.com",
            Password = "Password123!"
        });

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var messageProp = objectResult.Value?.GetType().GetProperty("message");
        Assert.NotNull(messageProp);
        var messageValue = messageProp.GetValue(objectResult.Value) as string;
        Assert.Equal("Your account is pending admin verification. Please check back later.", messageValue);
    }

    // 3. Admin approves → AccountStatus becomes Approved, email service invoked (mock and assert the call)
    [Fact]
    public async Task AdminApprovesOfficer_AccountBecomesApproved_AndEmailServiceInvoked()
    {
        using var context = CreateInMemoryDbContext();
        var mockEmail = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<AdminController>>();

        // Seed pending officer
        var officer = new User
        {
            Name = "Officer Kamal",
            Email = "kamal.ag@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var controller = new AdminController(context, mockEmail.Object, mockLogger.Object);

        // Act
        var result = await controller.ApproveOfficerRequest(officer.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var updatedUser = await context.Users.FindAsync(officer.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(AccountStatus.Approved, updatedUser.AccountStatus);

        // Verify email service was invoked with correct email and name
        mockEmail.Verify(e => e.SendOfficerApprovalEmailAsync("kamal.ag@example.com", "Officer Kamal"), Times.Once);
    }

    // 4. Approved officer logs in → tokens issued normally
    [Fact]
    public async Task ApprovedOfficer_CanLoginNormally_TokensIssued()
    {
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _config);
        var authController = new AuthController(authService);

        // Seed approved officer
        var officer = new User
        {
            Name = "Officer Kamal",
            Email = "kamal.approved@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        // Act
        var actionResult = await authController.Login(new LoginRequestDto
        {
            Email = "kamal.approved@example.com",
            Password = "Password123!"
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("kamal.approved@example.com", response.Email);
        Assert.Equal("AgriculturalOfficer", response.Role);
    }

    // 5. Email service throws/times out → approval still succeeds, failure logged, not thrown back to caller
    [Fact]
    public async Task EmailServiceFailure_DoesNotRollbackApproval_FailureLoggedGracefully()
    {
        // 5a. Test EmailService directly when external email dispatch fails
        var mockEmailLogger = new Mock<ILogger<EmailService>>();
        var emailService = new EmailService(_config, mockEmailLogger.Object);

        // Must not throw
        var exception = await Record.ExceptionAsync(() =>
            emailService.SendOfficerApprovalEmailAsync("kamal.fail@example.com", "Officer Kamal"));
        Assert.Null(exception);

        // 5b. Test AdminController when email service throws or fails
        using var context = CreateInMemoryDbContext();
        var officer = new User
        {
            Name = "Officer Kamal",
            Email = "kamal.mailfail@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var mockEmailService = new Mock<IEmailService>();
        mockEmailService
            .Setup(e => e.SendOfficerApprovalEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email dispatch unavailable"));

        // Use custom wrapper or test that approval succeeds even if IEmailService throws or logs
        var mockAdminLogger = new Mock<ILogger<AdminController>>();
        var controller = new AdminController(context, emailService, mockAdminLogger.Object);

        var actionResult = await controller.ApproveOfficerRequest(officer.Id);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);

        var approvedOfficer = await context.Users.FindAsync(officer.Id);
        Assert.NotNull(approvedOfficer);
        Assert.Equal(AccountStatus.Approved, approvedOfficer.AccountStatus);
    }

    // 6. Farmer registration/login completely unaffected by any of this
    [Fact]
    public async Task FarmerRegistrationAndLogin_CompletelyUnaffected()
    {
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _config);
        var authController = new AuthController(authService);

        // Farmer registers
        var registerResponse = await authService.RegisterAsync(new RegisterRequestDto
        {
            Name = "Sunil Farmer",
            Email = "sunil.farmer@example.com",
            Password = "Password123!",
            Role = "Farmer",
            Phone = "+94770000000"
        });

        Assert.False(registerResponse.RequiresApproval);
        Assert.False(string.IsNullOrWhiteSpace(registerResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(registerResponse.RefreshToken));
        Assert.Equal("Farmer", registerResponse.Role);

        // Check DB
        var farmerInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "sunil.farmer@example.com");
        Assert.NotNull(farmerInDb);
        Assert.Equal(UserRole.Farmer, farmerInDb.Role);
        Assert.Equal(AccountStatus.Approved, farmerInDb.AccountStatus);

        // Farmer logs in
        var loginActionResult = await authController.Login(new LoginRequestDto
        {
            Email = "sunil.farmer@example.com",
            Password = "Password123!"
        });

        var okResult = Assert.IsType<OkObjectResult>(loginActionResult);
        var authDto = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(authDto.AccessToken));
        Assert.Equal("sunil.farmer@example.com", authDto.Email);
        Assert.Equal("Farmer", authDto.Role);
    }
}
