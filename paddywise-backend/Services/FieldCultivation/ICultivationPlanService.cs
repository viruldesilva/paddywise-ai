using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public interface ICultivationPlanService
{
    /// <summary>
    /// Saves a Draft plan for one cycle and returns it; the agent does not run here — the
    /// caller enqueues the plan for <see cref="ProcessPlanAsync"/>. Null means the cycle does
    /// not exist — the controller turns that into a 404. A cycle belonging to someone else
    /// throws UnauthorizedAccessException; a cycle that already has a plan being generated,
    /// awaiting or holding approval throws InvalidOperationException.
    /// </summary>
    Task<CultivationPlanResponseDto?> RequestPlanAsync(int farmerId, int cycleId, string objective);

    /// <summary>
    /// Runs the Cultivation Planning Agent for a Draft plan, validates the result, dispatches
    /// its delegations and runs Component 4's second pass. Does nothing for a plan that is
    /// missing or no longer Draft. A failed agent run is not an exception: the plan ends up
    /// "ValidationFailed" with a readable message, and an AgentRunLog is always written.
    /// </summary>
    Task ProcessPlanAsync(int planId, CancellationToken ct);

    /// <summary>Fails a plan that is still Draft with the given message and logs a failed run.</summary>
    Task MarkFailedAsync(int planId, string message);

    /// <summary>
    /// Startup recovery: fails every Draft older than <paramref name="staleAfter"/> and returns
    /// the ids of the remaining Drafts, oldest first, for the caller to re-enqueue.
    /// </summary>
    Task<List<int>> RecoverDraftsAsync(TimeSpan staleAfter);

    Task<CultivationPlanResponseDto?> GetByIdAsync(int planId, int callerId, UserRole callerRole);

    /// <summary>Every plan for one cycle, newest first. Null when the cycle does not exist.</summary>
    Task<List<CultivationPlanResponseDto>?> GetForCycleAsync(int cycleId, int callerId, UserRole callerRole);

    /// <summary>
    /// Records an officer's verdict on a plan. Null means the plan does not exist. A plan that
    /// is not waiting for approval, an unparseable decision, and a rejection or revision request
    /// without a comment all throw InvalidOperationException.
    /// </summary>
    Task<CultivationPlanResponseDto?> ReviewAsync(int planId, int officerId, ReviewPlanDto request);

    /// <summary>The officer approval queue, newest first, optionally narrowed to one division.</summary>
    Task<List<PendingPlanSummaryDto>> GetPendingAsync(int? divisionId);
}
