using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.FieldCultivation;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.FieldCultivation;

public class FieldService : IFieldService
{
    private readonly ApplicationDbContext _context;

    public FieldService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FieldResponseDto>> GetMyFieldsAsync(int farmerId)
    {
        var fields = await _context.Fields
            .AsNoTracking()
            .Include(f => f.Division)
            .Include(f => f.Farmer)
            .Where(f => f.FarmerId == farmerId && f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync();

        return fields.Select(MapToResponse).ToList();
    }

    public async Task<List<FieldResponseDto>> GetFieldsByDivisionAsync(int divisionId)
    {
        var fields = await _context.Fields
            .AsNoTracking()
            .Include(f => f.Division)
            .Include(f => f.Farmer)
            .Where(f => f.DivisionId == divisionId && f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync();

        return fields.Select(MapToResponse).ToList();
    }

    public async Task<FieldResponseDto?> GetByIdAsync(int fieldId, int callerId, UserRole callerRole)
    {
        var field = await _context.Fields
            .AsNoTracking()
            .Include(f => f.Division)
            .Include(f => f.Farmer)
            .FirstOrDefaultAsync(f => f.Id == fieldId);

        if (field == null)
            return null;

        // Officers and admins may read any field; a farmer only reads their own.
        if (callerRole == UserRole.Farmer && field.FarmerId != callerId)
            throw new UnauthorizedAccessException("You do not have access to this field.");

        return MapToResponse(field);
    }

    public async Task<FieldResponseDto> CreateAsync(int farmerId, CreateFieldRequestDto request)
    {
        if (!await _context.Divisions.AnyAsync(d => d.Id == request.DivisionId))
            throw new InvalidOperationException("Division not found");

        var field = new Field
        {
            Name = request.Name,
            Area = request.Area,
            SoilType = request.SoilType,
            IrrigationType = request.IrrigationType,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DivisionId = request.DivisionId,
            FarmerId = farmerId
        };

        _context.Fields.Add(field);
        await _context.SaveChangesAsync();

        await _context.Entry(field).Reference(f => f.Division).LoadAsync();
        await _context.Entry(field).Reference(f => f.Farmer).LoadAsync();

        return MapToResponse(field);
    }

    public async Task<FieldResponseDto?> UpdateAsync(int fieldId, int farmerId, UpdateFieldRequestDto request)
    {
        var field = await _context.Fields
            .Include(f => f.Division)
            .Include(f => f.Farmer)
            .FirstOrDefaultAsync(f => f.Id == fieldId);

        if (field == null)
            return null;

        if (field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this field.");

        if (!await _context.Divisions.AnyAsync(d => d.Id == request.DivisionId))
            throw new InvalidOperationException("Division not found");

        field.Name = request.Name;
        field.Area = request.Area;
        field.SoilType = request.SoilType;
        field.IrrigationType = request.IrrigationType;
        field.Latitude = request.Latitude;
        field.Longitude = request.Longitude;
        field.DivisionId = request.DivisionId;
        field.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(field).Reference(f => f.Division).LoadAsync();

        return MapToResponse(field);
    }

    public async Task<bool> SoftDeleteAsync(int fieldId, int farmerId)
    {
        var field = await _context.Fields.FirstOrDefaultAsync(f => f.Id == fieldId);

        if (field == null)
            return false;

        if (field.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You do not have access to this field.");

        field.IsActive = false;
        field.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<DivisionResponseDto>> GetDivisionsAsync()
    {
        return await _context.Divisions
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DivisionResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                District = d.District,
                Province = d.Province
            })
            .ToListAsync();
    }

    private static FieldResponseDto MapToResponse(Field field) => new()
    {
        Id = field.Id,
        Name = field.Name,
        Area = field.Area,
        SoilType = field.SoilType,
        IrrigationType = field.IrrigationType,
        Latitude = field.Latitude,
        Longitude = field.Longitude,
        DivisionId = field.DivisionId,
        DivisionName = field.Division?.Name ?? string.Empty,
        FarmerId = field.FarmerId,
        FarmerName = field.Farmer?.Name ?? string.Empty,
        IsActive = field.IsActive,
        CreatedAt = field.CreatedAt,
        UpdatedAt = field.UpdatedAt
    };
}
