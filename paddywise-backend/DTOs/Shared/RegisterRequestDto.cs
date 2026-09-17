namespace PaddyWise.Api.DTOs.Shared;

public class RegisterRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Farmer", "AgriculturalOfficer", etc.
    public string? Phone { get; set; }
}