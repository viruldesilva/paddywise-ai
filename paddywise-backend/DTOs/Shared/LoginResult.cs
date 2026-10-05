using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.DTOs.Shared;

public class LoginResult
{
    public bool IsSuccess => AuthResponse != null;
    public bool IsBlocked => BlockedStatus != null;
    public AccountStatus? BlockedStatus { get; init; }
    public string? BlockedMessage { get; init; }
    public AuthResponseDto? AuthResponse { get; init; }

    public static LoginResult Success(AuthResponseDto authResponse) => new() { AuthResponse = authResponse };
    public static LoginResult InvalidCredentials() => new();
    public static LoginResult Blocked(AccountStatus status, string message) => new() 
    { 
        BlockedStatus = status, 
        BlockedMessage = message 
    };
}
