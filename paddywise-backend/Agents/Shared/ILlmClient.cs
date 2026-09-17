namespace PaddyWise.Api.Agents.Shared;

/// <summary>The one call every agent makes: prompt in, model's final text out.</summary>
public interface ILlmClient
{
    /// <summary>
    /// Runs a prompt to completion, executing any tools the model asks for along the way.
    /// <paramref name="toolExecutor"/> receives (toolName, argsJson) and returns the tool's
    /// result as JSON. Throws <see cref="LlmException"/> if the provider returns a non-2xx.
    /// </summary>
    Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<LlmToolDefinition> tools,
        Func<string, string, Task<string>> toolExecutor,
        CancellationToken ct);
}
