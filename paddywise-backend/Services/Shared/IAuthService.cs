using PaddyWise.Api.DTOs.Shared;

namespace PaddyWise.Api.Services.Shared;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResult> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);
}