using Microsoft.EntityFrameworkCore;
using PaddyWise.Api.Entities.FieldCultivation;
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
    public DbSet<PaddyWise.Api.Entities.CropResource.CropActivity> CropActivities => Set<PaddyWise.Api.Entities.CropResource.CropActivity>();//farmer crop activities

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

        modelBuilder.Entity<PaddyWise.Api.Entities.CropResource.CropActivity>(entity =>
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
    }
}