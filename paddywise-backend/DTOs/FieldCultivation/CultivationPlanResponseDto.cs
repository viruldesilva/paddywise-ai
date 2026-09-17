using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.Shared;

namespace PaddyWise.Api.DTOs.FieldCultivation;

/// <summary>A cultivation plan and the agent runs behind it.</summary>
public class CultivationPlanResponseDto
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>Null when the agent never produced a parseable plan.</summary>
    public CultivationPlanOutput? Plan { get; set; }

    /// <summary>Why the plan is not usable — empty while it still is.</summary>
    public List<string> ValidationErrors { get; set; } = new();

    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<AgentRunSummaryDto> AgentRuns { get; set; } = new();
}

/// <summary>One agent run behind a plan, for the audit trail on the officer's screen.</summary>
public class AgentRunSummaryDto
{
    public string AgentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int DurationMs { get; set; }
    public List<ToolCallRecord> ToolCalls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
