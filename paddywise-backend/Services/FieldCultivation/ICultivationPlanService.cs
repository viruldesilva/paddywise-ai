using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public interface ICultivationPlanService
{
    /// <summary>
    /// Runs the Cultivation Planning Agent for one cycle and stores the result. Null means the
    /// cycle does not exist — the controller turns that into a 404. A cycle belonging to
    /// someone else throws UnauthorizedAccessException; a cycle that already has a plan awaiting
    /// or holding approval throws InvalidOperationException. A failed agent run is not an
    /// exception: it comes back as a plan with Status "ValidationFailed".
    /// </summary>
    Task<CultivationPlanResponseDto?> RequestPlanAsync(int farmerId, int cycleId, string objective);

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
