using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>An agent-generated plan for one cultivation cycle, plus its approval trail.</summary>
public class CultivationPlan
{
    public int Id { get; set; }

    public int CultivationCycleId { get; set; }

    // The farmer who asked for the plan.
    public int RequestedByUserId { get; set; }

    /// <summary>What the farmer asked for, in their own words.</summary>
    public string Objective { get; set; } = string.Empty;

    /// <summary>The agent's plan. jsonb so the shape can evolve without a migration.</summary>
    public string PlanJson { get; set; } = "{}";

    public PlanStatus Status { get; set; }

    /// <summary>Failures from the deterministic validator; null until validation has run.</summary>
    public string? ValidationErrorsJson { get; set; }

    // The officer who reviewed it — null until someone picks it up.
    public int? OfficerId { get; set; }
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CultivationCycle CultivationCycle { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? Officer { get; set; }
}
