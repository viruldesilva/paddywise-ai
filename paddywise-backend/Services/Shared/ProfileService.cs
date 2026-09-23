using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.Shared;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.Shared;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ApplicationDbContext context, ILogger<ProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        // If the user is a farmer, compute their active farm footprint
        if (user.Role == UserRole.Farmer)
        {
            var fields = await _context.Fields
                .AsNoTracking()
                .Include(f => f.Division)
                .Where(f => f.FarmerId == userId && f.IsActive)
                .ToListAsync();

            profile.TotalFields = fields.Count;
            profile.TotalAcreage = fields.Sum(f => f.Area);
            profile.Divisions = fields
                .Where(f => f.Division != null && !string.IsNullOrWhiteSpace(f.Division.Name))
                .Select(f => f.Division!.Name)
                .Distinct()
                .ToList();

            var fieldIds = fields.Select(f => f.Id).ToList();
            if (fieldIds.Count > 0)
            {
                profile.ActiveCycles = await _context.CultivationCycles
                    .AsNoTracking()
                    .CountAsync(c => fieldIds.Contains(c.FieldId) &&
                                     (c.Status == CycleStatus.Active || c.Status == CycleStatus.Planned));
            }
        }

        return profile;
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        user.Name = request.Name.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user {UserId} ({Email}).", user.Id, user.Email);

        var updatedProfile = await GetProfileAsync(userId);
        return updatedProfile!;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new ArgumentException("Current password is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters long.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("New password and confirm password do not match.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password successfully changed for user {UserId} ({Email}).", user.Id, user.Email);
    }
}
