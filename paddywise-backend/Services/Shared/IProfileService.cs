using System.Threading.Tasks;
using PaddyWise.Api.DTOs.Shared;

namespace PaddyWise.Api.Services.Shared;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
}
