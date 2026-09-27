using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PaddyWise.Api.Services.Shared;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new InvalidOperationException("Invalid role specified.");

        var accountStatus = role == UserRole.AgriculturalOfficer
            ? AccountStatus.PendingApproval
            : AccountStatus.Approved;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            AccountStatus = accountStatus,
            Phone = request.Phone
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (role == UserRole.AgriculturalOfficer)
        {
            return new RegisterResponseDto
            {
                RequiresApproval = true,
                Message = "Your account is pending admin verification. You will be able to log in once approved.",
                AccessToken = null,
                RefreshToken = null,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        var authResponse = await GenerateAuthResponseAsync(user);
        return new RegisterResponseDto
        {
            RequiresApproval = false,
            Message = "Account registered successfully.",
            AccessToken = authResponse.AccessToken,
            RefreshToken = authResponse.RefreshToken,
            Name = authResponse.Name,
            Email = authResponse.Email,
            Role = authResponse.Role
        };
    }

    public async Task<LoginResult> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return LoginResult.InvalidCredentials();

        if (user.AccountStatus == AccountStatus.PendingApproval)
        {
            return LoginResult.Blocked(
                AccountStatus.PendingApproval,
                "Your account is pending admin verification. Please check back later."
            );
        }

        if (user.AccountStatus == AccountStatus.Rejected)
        {
            return LoginResult.Blocked(
                AccountStatus.Rejected,
                "Your account application was not approved. Please contact your administrator."
            );
        }

        var authResponse = await GenerateAuthResponseAsync(user);
        return LoginResult.Success(authResponse);
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return null; // caller returns 401 -> frontend redirects to login

        var user = await _context.Users.FindAsync(storedToken.UserId);
        if (user == null) return null;

        // Rotate: revoke the old one, issue a new pair
        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshTokenString();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!))
        });
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}