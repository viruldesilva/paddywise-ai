using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public interface IFieldService
{
    Task<List<FieldResponseDto>> GetMyFieldsAsync(int farmerId);
    Task<List<FieldResponseDto>> GetFieldsByDivisionAsync(int divisionId);
    Task<FieldResponseDto?> GetByIdAsync(int fieldId, int callerId, UserRole callerRole);
    Task<FieldResponseDto> CreateAsync(int farmerId, CreateFieldRequestDto request);
    Task<FieldResponseDto?> UpdateAsync(int fieldId, int farmerId, UpdateFieldRequestDto request);
    Task<bool> SoftDeleteAsync(int fieldId, int farmerId);
    Task<List<DivisionResponseDto>> GetDivisionsAsync();
}
