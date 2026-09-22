using System;
using System.ComponentModel.DataAnnotations;
using PaddyWise.Api.Entities.CropResource;

namespace PaddyWise.Api.DTOs.CropResource;

public class UpdateCropActivityRequestDto
{
    [Required]
    public CropActivityType ActivityType { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public string DetailsJson { get; set; } = "{}";
}
