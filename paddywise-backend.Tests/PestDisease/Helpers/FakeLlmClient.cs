using Moq;
using PaddyWise.Api.Agents.Shared;

namespace PaddyWise.Backend.Tests.PestDisease.Helpers;

/// <summary>
/// Builds a Mock&lt;ILlmClient&gt; for CropAnalysisAgent tests. Never calls real Gemini.
/// The scripted behaviour is a plain delegate so each test can decide exactly when (and
/// whether) to invoke the real toolExecutor callback it's handed — simulating the model
/// calling get_pest_knowledge — before returning its scripted raw JSON (or throwing).
/// </summary>
public static class FakeLlmClient
{
    /// <summary>The real tool name CropAnalysisAgent registers (its own const is private).</summary>
    public const string GetPestKnowledgeTool = "get_pest_knowledge";

    public delegate Task<string> OnCall(
        string systemPrompt,
        string userPrompt,
        Func<string, string, Task<string>> toolExecutor,
        int callIndex);

    /// <summary>
    /// One call per record: the exact systemPrompt/userPrompt CropAnalysisAgent built for
    /// that call. Index 0 is the first call, 1 is the one-retry call if there was one.
    /// </summary>
    public sealed record CapturedCall(string SystemPrompt, string UserPrompt);

    public static Mock<ILlmClient> Create(OnCall onCall, List<CapturedCall> capturedCalls)
    {
        var mock = new Mock<ILlmClient>();
        var callIndex = 0;

        mock.Setup(l => l.CompleteJsonWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmImagePart>>(),
                It.IsAny<IReadOnlyList<LlmToolDefinition>>(),
                It.IsAny<Func<string, string, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, IReadOnlyList<LlmImagePart>, IReadOnlyList<LlmToolDefinition>,
                Func<string, string, Task<string>>, CancellationToken>(
                async (systemPrompt, userPrompt, _, _, toolExecutor, _) =>
                {
                    capturedCalls.Add(new CapturedCall(systemPrompt, userPrompt));
                    var result = await onCall(systemPrompt, userPrompt, toolExecutor, callIndex);
                    callIndex++;
                    return result;
                });

        return mock;
    }

    /// <summary>Invokes get_pest_knowledge for one name, matching exactly what
    /// CropAnalysisAgent.ExecuteToolAsync expects as its argsJson shape.</summary>
    public static Task<string> LookUp(Func<string, string, Task<string>> toolExecutor, string pestName)
        => toolExecutor(GetPestKnowledgeTool, $"{{\"pestName\":\"{pestName}\"}}");
}
