using System.Threading.Channels;

namespace PaddyWise.Api.Services.FieldCultivation;

/// <summary>
/// Plan ids waiting for the planning agent. A plan request only saves a Draft and enqueues
/// its id; <see cref="PlanGenerationWorker"/> does the slow part outside the HTTP request.
/// </summary>
public interface IPlanGenerationQueue
{
    ValueTask EnqueueAsync(int planId, CancellationToken ct = default);

    ChannelReader<int> Reader { get; }
}

/// <summary>
/// In memory, so a restart loses whatever was queued — the worker re-enqueues every
/// surviving Draft on startup to make up for it.
/// </summary>
public sealed class PlanGenerationQueue : IPlanGenerationQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(int planId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(planId, ct);

    public ChannelReader<int> Reader => _channel.Reader;
}
