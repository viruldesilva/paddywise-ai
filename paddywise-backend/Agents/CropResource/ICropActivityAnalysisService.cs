using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PaddyWise.Api.Agents.CropResource;

public interface ICropActivityAnalysisService
{
    Task<CropActivityAnalysisOutput> AnalyzeActivitiesAsync(ActivityAnalysisInput input, int userId, CancellationToken ct = default);
    Task<AiChatResponseDto> ChatAboutActivitiesAsync(AiChatRequestDto request, int userId, CancellationToken ct = default);
    Task<ReviewRecommendationResponseDto> ReviewRecommendationAsync(int cycleId, ReviewRecommendationRequestDto request, int userId, string userRole, CancellationToken ct = default);
    Task<List<AgentAuditLogEntryDto>> GetAuditLogsAsync(int cycleId, CancellationToken ct = default);

    // Agricultural Officer Review Queue
    Task<List<CropActivityRecommendationDto>> GetPendingOfficerRecommendationsAsync(int? divisionId = null, CancellationToken ct = default);
    Task<List<CropActivityRecommendationDto>> GetCycleRecommendationsAsync(int cycleId, CancellationToken ct = default);
    Task<CropActivityRecommendationDto> OfficerReviewRecommendationAsync(int recommendationId, string decision, string? comment, int officerId, CancellationToken ct = default);
}
