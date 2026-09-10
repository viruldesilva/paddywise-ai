using PaddyWise.Api.DTOs.Shared;

namespace PaddyWise.Api.Services.Shared;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);
}