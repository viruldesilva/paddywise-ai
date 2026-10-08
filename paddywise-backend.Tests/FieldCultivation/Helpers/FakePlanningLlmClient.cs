using Moq;
using PaddyWise.Api.Agents.Shared;

namespace PaddyWise.Backend.Tests.FieldCultivation.Helpers;

/// <summary>
/// Builds a Mock&lt;ILlmClient&gt; for CultivationPlanningAgent tests. Never calls real Gemini.
/// Same shape as PestDisease's FakeLlmClient, but it scripts CompleteJsonAsync — the text-only
/// call the planning agent makes — rather than CompleteJsonWithImagesAsync. Each test decides
/// exactly when (and whether) to invoke the real toolExecutor callback it is handed, which
/// simulates the model calling get_cycle / get_stage_timeline before it answers.
/// </summary>
public static class FakePlanningLlmClient
{
    // The real tool names CultivationPlanningAgent registers (its own consts are private).
    public const string GetCycleTool = "get_cycle";
    public const string GetStageTimelineTool = "get_stage_timeline";
    public const string GetVarietyTool = "get_variety";
    public const string GetPreviousCyclesTool = "get_previous_cycles";

    public delegate Task<string> OnCall(
        string systemPrompt,
        string userPrompt,
        Func<string, string, Task<string>> toolExecutor,
        int callIndex);

    /// <summary>One call per record: the exact prompts the agent built for that call.</summary>
    public sealed record CapturedCall(string SystemPrompt, string UserPrompt);

    public static Mock<ILlmClient> Create(OnCall onCall, List<CapturedCall> capturedCalls)
    {
        var mock = new Mock<ILlmClient>();
        var callIndex = 0;

        mock.Setup(l => l.CompleteJsonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, IReadOnlyList<LlmToolDefinition>, Func<string, string, Task<string>>, CancellationToken>(
                async (systemPrompt, userPrompt, _, toolExecutor, _) =>
                {
                    capturedCalls.Add(new CapturedCall(systemPrompt, userPrompt));
                    var result = await onCall(systemPrompt, userPrompt, toolExecutor, callIndex);
                    callIndex++;
                    return result;
                });

        return mock;
    }

    /// <summary>Invokes one tool with a raw argsJson, exactly as the model would.</summary>
    public static Task<string> CallTool(
        Func<string, string, Task<string>> toolExecutor, string toolName, string argsJson)
        => toolExecutor(toolName, argsJson);

    /// <summary>The two calls the system prompt makes mandatory, for the run's own cycle.</summary>
    public static async Task CallRequiredToolsAsync(
        Func<string, string, Task<string>> toolExecutor, int cycleId)
    {
        await toolExecutor(GetCycleTool, $"{{\"cycleId\":{cycleId}}}");
        await toolExecutor(GetStageTimelineTool, $"{{\"cycleId\":{cycleId}}}");
    }
}
