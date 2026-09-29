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

    public string? FieldName { get; set; }
    public string? FarmerName { get; set; }
    public int? FarmerId { get; set; }
    public string? CycleName { get; set; }
    public string? DivisionName { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public decimal? FieldAreaAcres { get; set; }
}
