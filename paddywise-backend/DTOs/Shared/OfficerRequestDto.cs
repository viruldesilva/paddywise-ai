namespace PaddyWise.Api.DTOs.Shared;

public class OfficerRequestDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RejectOfficerRequestDto
{
    public string? Reason { get; set; }
}
