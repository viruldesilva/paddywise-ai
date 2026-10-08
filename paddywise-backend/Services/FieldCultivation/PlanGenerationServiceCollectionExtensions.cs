namespace PaddyWise.Api.Services.FieldCultivation;

public static class PlanGenerationServiceCollectionExtensions
{
    /// <summary>The queue plan requests are put on, and the background worker that drains it.</summary>
    public static IServiceCollection AddCultivationPlanGeneration(this IServiceCollection services)
    {
        services.AddSingleton<IPlanGenerationQueue, PlanGenerationQueue>();
        services.AddHostedService<PlanGenerationWorker>();
        return services;
    }
}
