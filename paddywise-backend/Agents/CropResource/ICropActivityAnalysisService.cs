using System.Threading;
using System.Threading.Tasks;

namespace PaddyWise.Api.Agents.CropResource;

public interface ICropActivityAnalysisService
{
    Task<CropActivityAnalysisOutput> AnalyzeActivitiesAsync(ActivityAnalysisInput input, int userId, CancellationToken ct = default);
    Task<AiChatResponseDto> ChatAboutActivitiesAsync(AiChatRequestDto request, int userId, CancellationToken ct = default);
}
