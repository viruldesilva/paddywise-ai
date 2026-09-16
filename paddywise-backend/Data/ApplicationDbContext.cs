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

        modelBuilder.Entity<Division>().HasData(
            new Division { Id = 1, Name = "Medirigiriya", District = "Polonnaruwa", Province = "North Central" },
            new Division { Id = 2, Name = "Nuwaragam Palatha", District = "Anuradhapura", Province = "North Central" },
            new Division { Id = 3, Name = "Tambuttegama", District = "Anuradhapura", Province = "North Central" },
            new Division { Id = 4, Name = "Ampara Central", District = "Ampara", Province = "Eastern" },
            new Division { Id = 5, Name = "Kurunegala West", District = "Kurunegala", Province = "North Western" }
        );
    }
}