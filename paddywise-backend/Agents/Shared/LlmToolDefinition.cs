namespace PaddyWise.Api.Agents.Shared;

/// <summary>
/// A tool offered to the model. <paramref name="ParametersJson"/> is a JSON Schema object
/// (the OpenAPI subset Gemini accepts), e.g.
/// <c>{"type":"object","properties":{"fieldId":{"type":"integer"}},"required":["fieldId"]}</c>.
/// </summary>
public sealed record LlmToolDefinition(string Name, string Description, string ParametersJson);
