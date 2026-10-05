using System;
using System.Collections.Generic;

namespace PaddyWise.Api.DTOs.Shared;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Dynamic farm footprint statistics for farmers
    public int TotalFields { get; set; }
    public decimal TotalAcreage { get; set; }
    public int ActiveCycles { get; set; }
    public List<string> Divisions { get; set; } = new();
}
