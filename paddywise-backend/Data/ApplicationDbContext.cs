using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Division> Divisions => Set<Division>();//Sri Lankan agrarian divisions
    public DbSet<User> Users => Set<User>();//for user authentication
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();//for JWT
    public DbSet<Variety> Varieties => Set<Variety>();//DOA paddy varieties
    public DbSet<CultivationCycle> CultivationCycles => Set<CultivationCycle>();//one season on one field
    public DbSet<GrowthStageLog> GrowthStageLogs => Set<GrowthStageLog>();//stage observations per cycle
    public DbSet<CultivationPlan> CultivationPlans => Set<CultivationPlan>();//agent-generated plan per cycle
    public DbSet<AgentRunLog> AgentRunLogs => Set<AgentRunLog>();//audit trail of agent runs
    public DbSet<CropObservation> CropObservations => Set<CropObservation>();//farmer-submitted pest/disease report
    public DbSet<PestDiseaseReport> PestDiseaseReports => Set<PestDiseaseReport>();//agent diagnosis per observation
    public DbSet<PestDiseaseKnowledge> PestDiseaseKnowledgeEntries => Set<PestDiseaseKnowledge>();//DOA-sourced reference data
    public DbSet<DiagnosisRunLog> DiagnosisRunLogs => Set<DiagnosisRunLog>();//audit trail of Crop Analysis agent runs
    public DbSet<CropActivity> CropActivities => Set<CropActivity>();//farmer crop activities
    public DbSet<CropActivityRecommendation> CropActivityRecommendations => Set<CropActivityRecommendation>();//agent recommendations awaiting officer review / executed

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();

        modelBuilder.Entity<Field>(entity =>
        {
            entity.Property(f => f.Area).HasPrecision(10, 2);
            entity.Property(f => f.IsActive).HasDefaultValue(true);

            entity.HasIndex(f => f.FarmerId);

            entity.HasOne(f => f.Farmer)
                .WithMany()
                .HasForeignKey(f => f.FarmerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Division)
                .WithMany()
                .HasForeignKey(f => f.DivisionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Quoted so Postgres keeps the column's casing instead of folding it to lowercase.
            entity.ToTable(t => t.HasCheckConstraint("CK_Fields_Area_Positive", "\"Area\" > 0"));
        });

        modelBuilder.Entity<Variety>(entity =>
        {
            entity.HasIndex(v => v.Name).IsUnique();
        });

        modelBuilder.Entity<CultivationCycle>(entity =>
        {
            // One cycle per field per season per year.
            entity.HasIndex(c => new { c.FieldId, c.Season, c.Year }).IsUnique();
            entity.HasIndex(c => c.Status);

            entity.HasOne(c => c.Field)
                .WithMany()
                .HasForeignKey(c => c.FieldId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Variety)
                .WithMany()
                .HasForeignKey(c => c.VarietyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GrowthStageLog>(entity =>
        {
            entity.HasIndex(g => g.CultivationCycleId);

            entity.HasOne(g => g.CultivationCycle)
                .WithMany()
                .HasForeignKey(g => g.CultivationCycleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict so removing a user cannot erase the observation trail.
            entity.HasOne(g => g.LoggedByUser)
                .WithMany()
                .HasForeignKey(g => g.LoggedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CultivationPlan>(entity =>
        {
            // jsonb, not text: the plan's shape will keep changing and Postgres can query it.
            entity.Property(p => p.PlanJson).HasColumnType("jsonb");
            entity.Property(p => p.ValidationErrorsJson).HasColumnType("jsonb");

            entity.HasIndex(p => p.CultivationCycleId);
            entity.HasIndex(p => p.Status);

            entity.HasOne(p => p.CultivationCycle)
                .WithMany()
                .HasForeignKey(p => p.CultivationCycleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.RequestedByUser)
                .WithMany()
                .HasForeignKey(p => p.RequestedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Officer)
                .WithMany()
                .HasForeignKey(p => p.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentRunLog>(entity =>
        {
            entity.Property(l => l.InputJson).HasColumnType("jsonb");
            entity.Property(l => l.ToolCallsJson).HasColumnType("jsonb");

            entity.HasIndex(l => l.CultivationPlanId);
            entity.HasIndex(l => l.CorrelationId);

            // SetNull so deleting a plan cannot erase the record that an agent ran.
            entity.HasOne(l => l.CultivationPlan)
                .WithMany()
                .HasForeignKey(l => l.CultivationPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CropActivity>(entity =>
        {
            entity.Property(a => a.DetailsJson).HasColumnType("jsonb");

            entity.HasIndex(a => a.CultivationCycleId);
            entity.HasIndex(a => a.ActivityType);

            entity.HasOne(a => a.CultivationCycle)
                .WithMany()
                .HasForeignKey(a => a.CultivationCycleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.LoggedByUser)
                .WithMany()
                .HasForeignKey(a => a.LoggedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CropActivityRecommendation>(entity =>
        {
            entity.HasIndex(r => r.CultivationCycleId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.OfficerId);
            entity.Property(r => r.CitationsJson).HasColumnType("jsonb");
            entity.Property(r => r.ExecutionPayloadJson).HasColumnType("jsonb");

            entity.HasOne(r => r.CultivationCycle)
                .WithMany()
                .HasForeignKey(r => r.CultivationCycleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Officer)
                .WithMany()
                .HasForeignKey(r => r.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ExecutedActivity)
                .WithMany()
                .HasForeignKey(r => r.ExecutedActivityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CropObservation>(entity =>
        {
            entity.HasIndex(o => o.CultivationCycleId);
            entity.HasIndex(o => o.ReportedByUserId);

            entity.HasOne(o => o.CultivationCycle)
                .WithMany()
                .HasForeignKey(o => o.CultivationCycleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict so removing a user cannot erase the observation trail.
            entity.HasOne(o => o.ReportedByUser)
                .WithMany()
                .HasForeignKey(o => o.ReportedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PestDiseaseReport>(entity =>
        {
            entity.Property(r => r.Confidence).HasPrecision(3, 2);

            entity.HasIndex(r => r.CropObservationId);
            entity.HasIndex(r => r.Status);

            entity.HasOne(r => r.CropObservation)
                .WithMany(o => o.Reports)
                .HasForeignKey(r => r.CropObservationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Officer)
                .WithMany()
                .HasForeignKey(r => r.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PestDiseaseKnowledge>(entity =>
        {
            entity.HasIndex(k => k.Name).IsUnique();
        });

        modelBuilder.Entity<DiagnosisRunLog>(entity =>
        {
            entity.Property(l => l.InputJson).HasColumnType("jsonb");
            entity.Property(l => l.ToolCallsJson).HasColumnType("jsonb");

            entity.HasIndex(l => l.CropObservationId);
            entity.HasIndex(l => l.CorrelationId);

            // SetNull so deleting an observation cannot erase the record that an agent ran.
            entity.HasOne(l => l.CropObservation)
                .WithMany()
                .HasForeignKey(l => l.CropObservationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Division>().HasData(
            new Division { Id = 1, Name = "Medirigiriya", District = "Polonnaruwa", Province = "North Central" },
            new Division { Id = 2, Name = "Nuwaragam Palatha", District = "Anuradhapura", Province = "North Central" },
            new Division { Id = 3, Name = "Tambuttegama", District = "Anuradhapura", Province = "North Central" },
            new Division { Id = 4, Name = "Ampara Central", District = "Ampara", Province = "Eastern" },
            new Division { Id = 5, Name = "Kurunegala West", District = "Kurunegala", Province = "North Western" }
        );

        // Durations below are working values and MUST be verified against
        // Department of Agriculture variety data before planning relies on them.
        modelBuilder.Entity<Variety>().HasData(
            new Variety { Id = 1, Name = "Bg 300", DurationDays = 90, AgeGroup = "3 month" },
            new Variety { Id = 2, Name = "Bg 352", DurationDays = 105, AgeGroup = "3.5 month" },
            new Variety { Id = 3, Name = "Bg 360", DurationDays = 105, AgeGroup = "3.5 month" },
            new Variety { Id = 4, Name = "At 362", DurationDays = 105, AgeGroup = "3.5 month" },
            new Variety { Id = 5, Name = "Bg 94-1", DurationDays = 105, AgeGroup = "3.5 month" },
            new Variety { Id = 6, Name = "Bg 359", DurationDays = 105, AgeGroup = "3.5 month" },
            new Variety { Id = 7, Name = "At 307", DurationDays = 90, AgeGroup = "3 month" },
            new Variety { Id = 8, Name = "Bw 367", DurationDays = 105, AgeGroup = "3.5 month" }
        );

        // Symptoms/management text below are working values from general agronomic references
        // and MUST be verified against current Sri Lanka Department of Agriculture publications
        // before the diagnosis agent's matches are treated as authoritative.
        modelBuilder.Entity<PestDiseaseKnowledge>().HasData(
            new PestDiseaseKnowledge
            {
                Id = 1,
                Name = "Thrips",
                Symptoms = "Silvery streaks and curling on young leaves; stunted growth in seedlings.",
                FavorableConditions = "Dry weather, drought-stressed nurseries.",
                CropStages = "Nursery, Tillering",
                ManagementGuidance = "Maintain adequate field water level; apply an approved insecticide only once infestation passes the economic threshold.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 2,
                Name = "Brown Planthopper",
                Symptoms = "Yellowing and drying of leaves from the base upward (\"hopperburn\"); stunted, wilting tillers.",
                FavorableConditions = "Dense planting, excess nitrogen, continuous flooding, high humidity.",
                CropStages = "Tillering, PanicleInitiation",
                ManagementGuidance = "Avoid excess nitrogen; alternate wetting and drying; favor resistant varieties; targeted insecticide only at economic threshold.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 3,
                Name = "Yellow Stem Borer",
                Symptoms = "Dead heart (dried central shoot) during vegetative growth; whitehead (empty, upright panicle) at the reproductive stage.",
                FavorableConditions = "Continuous rice cropping without fallow, high nitrogen.",
                CropStages = "Tillering, Flowering",
                ManagementGuidance = "Remove and destroy egg masses and post-harvest stubble; use light traps; targeted insecticide once dead-heart incidence passes threshold.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 4,
                Name = "Rice Leaf Folder",
                Symptoms = "Leaves folded longitudinally and webbed together; white/transparent streaks where larvae scrape and feed inside the fold.",
                FavorableConditions = "High nitrogen, dense canopy, high humidity.",
                CropStages = "Tillering, PanicleInitiation",
                ManagementGuidance = "Balanced nitrogen application; conserve natural enemies; insecticide only above the recommended damage threshold.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 5,
                Name = "Rice Sheath Mite",
                Symptoms = "Brown to black lesions on the leaf sheath near the waterline; can cause unfilled or discolored grains.",
                FavorableConditions = "Warm, humid conditions and dense planting.",
                CropStages = "PanicleInitiation, Flowering",
                ManagementGuidance = "Avoid excess nitrogen and overly dense planting; miticide only under severe, confirmed infestation.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 6,
                Name = "Rice Gall Midge",
                Symptoms = "Affected tiller produces a tubular \"silvershoot\"/onion-leaf gall instead of a normal leaf whorl and no panicle.",
                FavorableConditions = "High humidity, shaded or low-lying fields, continuous rice cropping.",
                CropStages = "Nursery, Tillering",
                ManagementGuidance = "Synchronize planting across the area; use resistant varieties; remove wild grasses acting as alternate hosts.",
                Source = "Sri Lanka Department of Agriculture"
            },
            new PestDiseaseKnowledge
            {
                Id = 7,
                Name = "Sheath Rot",
                Symptoms = "Reddish-brown lesions on the flag leaf sheath enclosing the panicle; panicle may fail to emerge fully or grains are discolored.",
                FavorableConditions = "High humidity, excess nitrogen, dense planting.",
                CropStages = "PanicleInitiation, Flowering",
                ManagementGuidance = "Avoid excess nitrogen; ensure adequate spacing/drainage for airflow; treat seed and apply fungicide at booting stage if severe.",
                Source = "Sri Lanka Department of Agriculture"
            }
        );
    }
}