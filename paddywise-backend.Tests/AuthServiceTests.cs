using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Api.Services.Shared;
using Xunit;

namespace PaddyWise.Backend.Tests;

[Trait("Component", "ReportingApproval")]
public class AuthServiceTests
{
    private readonly IConfiguration _config;

    public AuthServiceTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "super-secret-paddywise-jwt-key-for-unit-testing-32chars" },
            { "Jwt:Issuer", "PaddyWiseApi" },
            { "Jwt:Audience", "PaddyWiseClient" },
            { "Jwt:AccessTokenExpiryMinutes", "20" },
            { "Jwt:RefreshTokenExpiryDays", "7" }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AuthTestDb_" + Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // 1. Normal Case: Register AgriculturalOfficer -> AccountStatus PendingApproval, no tokens returned
    [Fact]
    public async Task Register_AgriculturalOfficer_SetsPendingApprovalAndReturnsNoTokens()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var request = new RegisterRequestDto
        {
            Name = "Officer Samantha",
            Email = "samantha.officer@example.com",
            Password = "Password123!",
            Role = "AgriculturalOfficer",
            Phone = "+94771234567"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RequiresApproval);
        Assert.Equal("Your account is pending admin verification. You will be able to log in once approved.", result.Message);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Equal("AgriculturalOfficer", result.Role);

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(userInDb);
        Assert.Equal(AccountStatus.PendingApproval, userInDb.AccountStatus);
        Assert.Equal(UserRole.AgriculturalOfficer, userInDb.Role);
    }

    // 2. Invalid Case: Register duplicate email -> throws InvalidOperationException
    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var existingUser = new User
        {
            Name = "Existing User",
            Email = "duplicate@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Name = "Another User",
            Email = "duplicate@example.com",
            Password = "AnotherPassword123!",
            Role = "Farmer"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));
        Assert.Equal("A user with this email already exists.", ex.Message);
    }

    // 3. Edge Case: Login PendingApproval officer -> blocked with correct message and status
    [Fact]
    public async Task Login_PendingApprovalOfficer_IsBlockedWithDescriptiveMessage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var officer = new User
        {
            Name = "Pending Officer",
            Email = "officer.pending@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "officer.pending@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsBlocked);
        Assert.False(result.IsSuccess);
        Assert.Equal(AccountStatus.PendingApproval, result.BlockedStatus);
        Assert.Equal("Your account is pending admin verification. Please check back later.", result.BlockedMessage);
        Assert.Null(result.AuthResponse);
    }

    // 4. Edge Case: Login Rejected officer -> blocked with correct message and status
    [Fact]
    public async Task Login_RejectedOfficer_IsBlockedWithDescriptiveMessage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var officer = new User
        {
            Name = "Rejected Officer",
            Email = "officer.rejected@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Rejected
        };
        context.Users.Add(officer);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "officer.rejected@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsBlocked);
        Assert.False(result.IsSuccess);
        Assert.Equal(AccountStatus.Rejected, result.BlockedStatus);
        Assert.Equal("Your account application was not approved. Please contact your administrator.", result.BlockedMessage);
        Assert.Null(result.AuthResponse);
    }

    // 5. Invalid Case: Login wrong password -> InvalidCredentials, no tokens
    [Fact]
    public async Task Login_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var user = new User
        {
            Name = "John Farmer",
            Email = "john.farmer@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "john.farmer@example.com",
            Password = "WrongPassword999!"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsBlocked);
        Assert.Null(result.AuthResponse);
    }

    // 6. Normal Case: Login Approved user -> tokens issued correctly
    [Fact]
    public async Task Login_ApprovedUser_IssuesTokensSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var user = new User
        {
            Name = "Officer Kamal",
            Email = "kamal.officer@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            Role = UserRole.AgriculturalOfficer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "kamal.officer@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsBlocked);
        Assert.NotNull(result.AuthResponse);
        Assert.False(string.IsNullOrWhiteSpace(result.AuthResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.AuthResponse.RefreshToken));
        Assert.Equal("kamal.officer@example.com", result.AuthResponse.Email);
        Assert.Equal("AgriculturalOfficer", result.AuthResponse.Role);
    }

    // 7. Boundary Case: Refresh expired or revoked token -> returns null
    [Fact]
    public async Task Refresh_ExpiredOrRevokedToken_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var user = new User
        {
            Name = "Active User",
            Email = "active@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(user);

        // Token 1: Revoked
        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "revoked_token_sample",
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.RefreshTokens.Add(revokedToken);

        // Token 2: Expired
        var expiredToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "expired_token_sample",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.RefreshTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        // Act
        var resultRevoked = await service.RefreshAsync("revoked_token_sample");
        var resultExpired = await service.RefreshAsync("expired_token_sample");
        var resultNonExistent = await service.RefreshAsync("non_existent_token");

        // Assert
        Assert.Null(resultRevoked);
        Assert.Null(resultExpired);
        Assert.Null(resultNonExistent);
    }

    // 8. Normal Case: Refresh valid token -> new rotated pair issued, old marked revoked
    [Fact]
    public async Task Refresh_ValidToken_IssuesNewPairAndRevokesOldToken()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, _config);
        var user = new User
        {
            Id = 15,
            Name = "Rotation User",
            Email = "rotate@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Farmer,
            AccountStatus = AccountStatus.Approved
        };
        context.Users.Add(user);

        var originalToken = new RefreshToken
        {
            UserId = 15,
            Token = "valid_refresh_token_to_rotate",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(5)
        };
        context.RefreshTokens.Add(originalToken);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RefreshAsync("valid_refresh_token_to_rotate");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual("valid_refresh_token_to_rotate", result.RefreshToken);

        // Verify database state: old token is now revoked
        var oldTokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == "valid_refresh_token_to_rotate");
        Assert.NotNull(oldTokenInDb);
        Assert.True(oldTokenInDb.IsRevoked);

        // Verify new token exists in DB and is active
        var newTokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == result.RefreshToken);
        Assert.NotNull(newTokenInDb);
        Assert.False(newTokenInDb.IsRevoked);
        Assert.Equal(15, newTokenInDb.UserId);
    }
}
