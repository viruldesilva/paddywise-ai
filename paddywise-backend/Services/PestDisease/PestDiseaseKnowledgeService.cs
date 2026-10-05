using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Data;
using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.PestDisease;

namespace PaddyWise.Api.Services.PestDisease;

public class PestDiseaseKnowledgeService : IPestDiseaseKnowledgeService
{
    private readonly ApplicationDbContext _context;

    public PestDiseaseKnowledgeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PestDiseaseKnowledgeResponseDto>> GetAllAsync()
    {
        var entries = await _context.PestDiseaseKnowledgeEntries
            .AsNoTracking()
            .OrderBy(k => k.Name)
            .ToListAsync();

        return entries.Select(MapToResponse).ToList();
    }

    public async Task<PestDiseaseKnowledgeResponseDto?> GetByIdAsync(int id)
    {
        var entry = await _context.PestDiseaseKnowledgeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);

        return entry == null ? null : MapToResponse(entry);
    }

    public async Task<PestDiseaseKnowledgeResponseDto> CreateAsync(SavePestDiseaseKnowledgeRequestDto request)
    {
        var name = request.Name.Trim();
        var category = ParseEnum<PestDiseaseCategory>(
            request.Category, "Category must be Pest or Disease.");

        if (await NameTakenAsync(name, excludingId: null))
            throw new InvalidOperationException($"A knowledge base entry named '{name}' already exists.");

        var entry = new PestDiseaseKnowledge
        {
            Name = name,
            Category = category,
            Symptoms = request.Symptoms.Trim(),
            FavorableConditions = Normalize(request.FavorableConditions),
            CropStages = Normalize(request.CropStages),
            ManagementGuidance = request.ManagementGuidance.Trim(),
            Source = request.Source.Trim()
        };

        _context.PestDiseaseKnowledgeEntries.Add(entry);
        await _context.SaveChangesAsync();

        return MapToResponse(entry);
    }

    public async Task<PestDiseaseKnowledgeResponseDto?> UpdateAsync(int id, SavePestDiseaseKnowledgeRequestDto request)
    {
        var entry = await _context.PestDiseaseKnowledgeEntries.FirstOrDefaultAsync(k => k.Id == id);
        if (entry == null)
            return null;

        var name = request.Name.Trim();
        var category = ParseEnum<PestDiseaseCategory>(
            request.Category, "Category must be Pest or Disease.");

        if (await NameTakenAsync(name, excludingId: id))
            throw new InvalidOperationException($"A knowledge base entry named '{name}' already exists.");

        entry.Name = name;
        entry.Category = category;
        entry.Symptoms = request.Symptoms.Trim();
        entry.FavorableConditions = Normalize(request.FavorableConditions);
        entry.CropStages = Normalize(request.CropStages);
        entry.ManagementGuidance = request.ManagementGuidance.Trim();
        entry.Source = request.Source.Trim();
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(entry);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entry = await _context.PestDiseaseKnowledgeEntries.FirstOrDefaultAsync(k => k.Id == id);
        if (entry == null)
            return false;

        // No FK from PestDiseaseReport.PossibleIssue — it is a text snapshot, not a reference —
        // so removing an entry cannot orphan a past diagnosis. Only future agent runs lose it.
        _context.PestDiseaseKnowledgeEntries.Remove(entry);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> NameTakenAsync(string name, int? excludingId)
    {
        var query = _context.PestDiseaseKnowledgeEntries.Where(k => k.Name.ToLower() == name.ToLower());

        if (excludingId.HasValue)
            query = query.Where(k => k.Id != excludingId.Value);

        return await query.AnyAsync();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static T ParseEnum<T>(string value, string message) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidOperationException(message);

        return parsed;
    }

    private static PestDiseaseKnowledgeResponseDto MapToResponse(PestDiseaseKnowledge entry) => new()
    {
        Id = entry.Id,
        Name = entry.Name,
        Category = entry.Category.ToString(),
        Symptoms = entry.Symptoms,
        FavorableConditions = entry.FavorableConditions,
        CropStages = entry.CropStages,
        ManagementGuidance = entry.ManagementGuidance,
        Source = entry.Source,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };
}
