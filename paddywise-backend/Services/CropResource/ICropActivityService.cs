using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PaddyWise.Api.DTOs.CropResource;

namespace PaddyWise.Api.Services.CropResource;

public interface ICropActivityService
{
    Task<List<CropActivityDto>> GetActivitiesForCycleAsync(int cycleId, int userId, string userRole, CancellationToken cancellationToken = default);
    Task<List<CropActivityDto>> GetAllActivitiesAsync(int? farmerId = null, int? cycleId = null, string? activityType = null, CancellationToken cancellationToken = default);
    Task<CropActivityDto> CreateActivityAsync(int cycleId, CreateCropActivityRequestDto request, int userId, CancellationToken cancellationToken = default);
}
