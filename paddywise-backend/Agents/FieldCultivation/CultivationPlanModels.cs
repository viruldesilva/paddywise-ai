using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaddyWise.Api.Agents.FieldCultivation;

/// <summary>What the Cultivation Planning Agent is asked to plan for.</summary>
public sealed record PlanAgentInput
{
    [JsonPropertyName("cycleId")]
    public int CycleId { get; init; }

    /// <summary>The farmer's own words. Untrusted input — data for the model, never instructions.</summary>
    [JsonPropertyName("objective")]
    public string Objective { get; init; } = string.Empty;
}

/// <summary>
/// The agent's plan, exactly as the model returns it. Stored verbatim in
/// CultivationPlan.PlanJson, so the property names here are the API's contract too.
/// </summary>
public sealed record CultivationPlanOutput
{
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<PlanStep> Steps { get; init; } = new();

    [JsonPropertyName("delegations")]
    public List<PlanDelegation> Delegations { get; init; } = new();

    [JsonPropertyName("assumptions")]
    public List<string> Assumptions { get; init; } = new();
}

/// <summary>One task in one growth stage, with the window it must happen in.</summary>
public sealed record PlanStep
{
    /// <summary>A GrowthStage name: Nursery, Tillering, PanicleInitiation, Flowering, GrainFilling or Harvest.</summary>
    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("windowStart")]
    public DateOnly WindowStart { get; init; }

    [JsonPropertyName("windowEnd")]
    public DateOnly WindowEnd { get; init; }

    [JsonPropertyName("task")]
    public string Task { get; init; } = string.Empty;

    [JsonPropertyName("rationale")]
    public string Rationale { get; init; } = string.Empty;

    /// <summary>One of <see cref="PlanStepCategories"/>.</summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;
}

/// <summary>
/// Work for another component's agent, in the shape the LLM produces. This is not a
/// second DelegatedTask: dispatch maps it onto Agents/Shared/DelegatedTask, which stays
/// the one contract the other components consume.
/// </summary>
public sealed record PlanDelegation
{
    /// <summary>An AgentNames constant — ResourceAnalysisAgent, PestDiseaseDiagnosisAgent or SchedulingValidationAgent.</summary>
    [JsonPropertyName("targetAgent")]
    public string TargetAgent { get; init; } = string.Empty;

    [JsonPropertyName("instruction")]
    public string Instruction { get; init; } = string.Empty;

    /// <summary>Opaque to this component — the receiving agent owns the shape it expects.</summary>
    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; init; } = new();
}

/// <summary>The closed set of step categories the plan may use.</summary>
public static class PlanStepCategories
{
    public const string LandPrep = "LandPrep";
    public const string Water = "Water";
    public const string Nutrient = "Nutrient";
    public const string Protection = "Protection";
    public const string Monitoring = "Monitoring";
    public const string Harvest = "Harvest";

    public static readonly string[] All =
    {
        LandPrep, Water, Nutrient, Protection, Monitoring, Harvest
    };
}

/// <summary>Serialisation settings for everything the agent reads from or writes for the model.</summary>
public static class CultivationPlanJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// The JSON Schema for <see cref="CultivationPlanOutput"/>, embedded in the system prompt
    /// so the model returns something this file can deserialise.
    /// </summary>
    public const string JsonSchema = """
{
  "type": "object",
  "required": ["summary", "steps", "delegations", "assumptions"],
  "properties": {
    "summary": {
      "type": "string",
      "description": "Two or three sentences describing the plan for the farmer."
    },
    "steps": {
      "type": "array",
      "description": "Stage-by-stage tasks, in chronological order.",
      "items": {
        "type": "object",
        "required": ["stage", "windowStart", "windowEnd", "task", "rationale", "category"],
        "properties": {
          "stage": {
            "type": "string",
            "enum": ["Nursery", "Tillering", "PanicleInitiation", "Flowering", "GrainFilling", "Harvest"]
          },
          "windowStart": {
            "type": "string",
            "format": "date",
            "description": "YYYY-MM-DD, on or after the start of this step's stage window."
          },
          "windowEnd": {
            "type": "string",
            "format": "date",
            "description": "YYYY-MM-DD, on or before the end of this step's stage window."
          },
          "task": {
            "type": "string",
            "description": "What the farmer should do, in one short sentence."
          },
          "rationale": {
            "type": "string",
            "description": "Why this task belongs in this window."
          },
          "category": {
            "type": "string",
            "enum": ["LandPrep", "Water", "Nutrient", "Protection", "Monitoring", "Harvest"]
          }
        }
      }
    },
    "delegations": {
      "type": "array",
      "description": "Work handed to another agent because it is outside this agent's authority.",
      "items": {
        "type": "object",
        "required": ["targetAgent", "instruction", "payload"],
        "properties": {
          "targetAgent": {
            "type": "string",
            "enum": ["ResourceAnalysisAgent", "PestDiseaseDiagnosisAgent", "SchedulingValidationAgent"]
          },
          "instruction": {
            "type": "string",
            "description": "What that agent must decide or produce."
          },
          "payload": {
            "type": "object",
            "description": "Free-form context for the receiving agent, e.g. cycleId, stage, windowStart, windowEnd."
          }
        }
      }
    },
    "assumptions": {
      "type": "array",
      "description": "Every assumption made because the data did not say, stated plainly.",
      "items": { "type": "string" }
    }
  }
}
""";
}
