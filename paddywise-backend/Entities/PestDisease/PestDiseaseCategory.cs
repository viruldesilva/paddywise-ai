namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>Which half of the knowledge base an entry belongs to — used to narrow
/// CropAnalysisAgent's system prompt to only the relevant names for a given observation's
/// ObservationType, instead of listing every entry on every run.</summary>
public enum PestDiseaseCategory
{
    Pest = 0,
    Disease = 1
}
