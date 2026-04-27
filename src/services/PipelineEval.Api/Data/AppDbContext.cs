using Microsoft.EntityFrameworkCore;

namespace PipelineEval.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.UserSub).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.UserSub);
            e.Property(t => t.Title).HasMaxLength(500).IsRequired();
            e.Property(t => t.Notes).HasMaxLength(4000);
            e.Property(t => t.CatImageObjectKey).HasMaxLength(1024);
        });
    }
}
