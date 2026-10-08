namespace PaddyWise.Api.Services.FieldCultivation;

/// <summary>
/// Runs queued plan requests one at a time — the planning agent, its delegations and
/// Component 4's second validation pass — so a run that takes minutes never holds an HTTP
/// request open. Every job gets its own DI scope, so its DbContext, ILlmClient and agents
/// are never shared with a request thread or with another job.
/// </summary>
public sealed class PlanGenerationWorker : BackgroundService
{
    /// <summary>
    /// A Draft older than this at startup is not retried: whatever the farmer asked for that
    /// long ago is failed with a message instead, so they can ask again.
    /// </summary>
    public static readonly TimeSpan StaleDraftAge = TimeSpan.FromMinutes(30);

    private readonly IPlanGenerationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlanGenerationWorker> _logger;

    public PlanGenerationWorker(
        IPlanGenerationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PlanGenerationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the host finish starting before touching the database.
        await Task.Yield();

        await RecoverDraftsAsync(stoppingToken);

        try
        {
            await foreach (var planId in _queue.Reader.ReadAllAsync(stoppingToken))
                await ProcessAsync(planId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down. Anything still queued is a Draft and is picked up on the next start.
        }
    }

    /// <summary>
    /// The queue does not survive a restart, so a Draft found at startup was orphaned by one.
    /// The app runs as a single instance, so every such Draft is re-enqueued — except those
    /// older than <see cref="StaleDraftAge"/>, which are failed.
    /// </summary>
    private async Task RecoverDraftsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICultivationPlanService>();

            var requeue = await service.RecoverDraftsAsync(StaleDraftAge);

            foreach (var planId in requeue)
                await _queue.EnqueueAsync(planId, stoppingToken);

            if (requeue.Count > 0)
                _logger.LogInformation("Re-enqueued {Count} Draft plan(s) left over from a restart.", requeue.Count);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // A worker that throws stops the whole host; a failed recovery must not.
            _logger.LogError(ex, "Recovering Draft plans at startup failed.");
        }
    }

    private async Task ProcessAsync(int planId, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICultivationPlanService>();
            await service.ProcessPlanAsync(planId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // ProcessPlanAsync records the failures it expects. This is the last resort, so one
            // bad job can neither kill the worker nor leave its plan in Draft for good.
            _logger.LogError(ex, "Generating plan {PlanId} failed unexpectedly.", planId);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ICultivationPlanService>();
                await service.MarkFailedAsync(planId, "Something went wrong while generating this plan. Please request a new one.");
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx, "Could not mark plan {PlanId} as failed.", planId);
            }
        }
    }
}
