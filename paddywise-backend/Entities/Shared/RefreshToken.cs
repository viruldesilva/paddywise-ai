namespace PaddyWise.Api.Entities.Shared;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }           // FK -> Users.Id
    public string Token { get; set; } = string.Empty;   // candidate key (unique)
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}