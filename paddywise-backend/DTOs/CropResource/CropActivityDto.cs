using System;
using PaddyWise.Api.Entities.CropResource;

namespace PaddyWise.Api.DTOs.CropResource;

public class CropActivityDto
{
    public int Id { get; set; }
    public int CultivationCycleId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string DetailsJson { get; set; } = "{}";
    
    public int LoggedByUserId { get; set; }
    public string LoggedByUserName { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; }
}
