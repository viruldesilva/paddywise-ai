namespace PaddyWise.Api.Agents.Shared;

/// <summary>
/// One agent: takes a typed input, returns a typed output plus the trail of tool calls
/// it made getting there. Implementations are registered in <c>Program.cs</c>.
/// </summary>
public interface IAgent<TInput, TOutput>
{
    string Name { get; }

    Task<AgentResult<TOutput>> RunAsync(TInput input, AgentContext ctx, CancellationToken ct);
}

/// <summary>The outcome of a single agent run, successful or not.</summary>
public class AgentResult<T>
{
    public bool Success { get; set; }
    public T? Output { get; set; }
    public string? Error { get; set; }

    /// <summary>Every tool the agent invoked, in order — persisted to AgentRunLog.ToolCallsJson.</summary>
    public List<ToolCallRecord> ToolCalls { get; set; } = new();

    public TimeSpan Duration { get; set; }
}

/// <summary>Who asked for the run, and the id that ties every log line of it together.</summary>
public class AgentContext
{
    public int RequestedByUserId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>A single tool invocation made during an agent run.</summary>
public class ToolCallRecord
{
    public string Tool { get; set; } = string.Empty;
    public string ArgsJson { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
}
