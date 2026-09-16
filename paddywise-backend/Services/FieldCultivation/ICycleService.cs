using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public interface ICycleService
{
    // Null means the field does not exist (or has been soft-deleted) — the controller turns that into a 404.
    Task<CycleResponseDto?> StartCycleAsync(int farmerId, CreateCycleRequestDto request);
    Task<List<CycleResponseDto>> GetMyCyclesAsync(int farmerId, int? fieldId = null);
    Task<CycleResponseDto?> GetByIdAsync(int cycleId, int callerId, UserRole callerRole);
    Task<CycleResponseDto?> LogStageAsync(int cycleId, int callerId, UserRole callerRole, LogStageRequestDto request);
    Task<CycleResponseDto?> UpdateStatusAsync(int cycleId, int farmerId, CycleStatus status);
    Task<List<VarietyResponseDto>> GetVarietiesAsync();
}
