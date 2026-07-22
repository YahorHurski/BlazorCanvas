using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Data;

/// <summary>
/// The EF Core model for the two-table schema (D-12). EF Core infers none of the CHECK
/// constraints, the table comment, or the index name — every one of them is configured
/// explicitly below (D-42). This is the authoritative DDL transcription of `CONSTRAINT-schema`
/// in `.planning/intel/constraints.md`; never implement from D-12's own stale DDL sketch.
/// </summary>
public class CanvasDbContext : DbContext
{
    public CanvasDbContext(DbContextOptions<CanvasDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(u => u.Username)
                .HasColumnName("username")
                .HasColumnType("text")
                .IsRequired();
            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.Property(u => u.Password)
                .HasColumnName("password")
                .HasColumnType("text")
                .IsRequired();
        });

    }
}
