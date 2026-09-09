using Microsoft.EntityFrameworkCore; 
namespace PaddyWise.Api.Data; 
public class ApplicationDbContext : DbContext { 
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {

     } 
    // DbSets go here as you add entities, e.g.: 
    public DbSet<Field> Fields => Set<Field>(); 
}