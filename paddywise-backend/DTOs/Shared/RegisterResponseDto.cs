namespace PaddyWise.Api.DTOs.Shared;

public class RegisterResponseDto
{
    public bool RequiresApproval { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}
