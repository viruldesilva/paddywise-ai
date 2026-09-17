using System.Diagnostics;

namespace PaddyWise.Api.Agents.Shared;

// Placeholders so the Cultivation Planning Agent can delegate today. Each is registered
// against its AgentNames key in Program.cs; the owning component swaps in the real
// implementation by registering against the same key.

/// <summary>Component 2 — Crop Activity & Resource Management.</summary>
public sealed class ResourceAnalysisAgentStub : IAgent<DelegatedTask, DelegatedTaskResult>
{
    public string Name => AgentNames.ResourceAnalysis;

    public Task<AgentResult<DelegatedTaskResult>> RunAsync(
        DelegatedTask input,
        AgentContext ctx,
        CancellationToken ct) =>
        Task.FromResult(StubAgentResult.For(
            "stub — to be implemented by the Crop Activity & Resource Management owner"));
}

/// <summary>Component 3 — Pest & Disease Monitoring.</summary>
public sealed class PestDiseaseDiagnosisAgentStub : IAgent<DelegatedTask, DelegatedTaskResult>
{
    public string Name => AgentNames.PestDiseaseDiagnosis;

    public Task<AgentResult<DelegatedTaskResult>> RunAsync(
        DelegatedTask input,
        AgentContext ctx,
        CancellationToken ct) =>
        Task.FromResult(StubAgentResult.For(
            "stub — to be implemented by the Pest & Disease Monitoring owner"));
}

/// <summary>Component 4 — Reporting, Dashboards & Approval.</summary>
public sealed class SchedulingValidationAgentStub : IAgent<DelegatedTask, DelegatedTaskResult>
{
    public string Name => AgentNames.SchedulingValidation;

    public Task<AgentResult<DelegatedTaskResult>> RunAsync(
        DelegatedTask input,
        AgentContext ctx,
        CancellationToken ct) =>
        Task.FromResult(StubAgentResult.For(
            "stub — to be implemented by the Reporting, Dashboards & Approval owner"));
}

internal static class StubAgentResult
{
    public static AgentResult<DelegatedTaskResult> For(string note) => new()
    {
        Success = true,
        Output = new DelegatedTaskResult { Note = note },
        Duration = TimeSpan.Zero
    };
}
