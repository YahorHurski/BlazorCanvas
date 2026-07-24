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

    public DbSet<Figure> Figures => Set<Figure>();

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

        modelBuilder.Entity<Figure>(entity =>
        {
            entity.ToTable("figures", t =>
            {
                // type must be one of the four known lowercase literals (D-46)
                t.HasCheckConstraint(
                    "figures_type_is_known",
                    "type IN ('line','rectangle','circle','triangle')");

                t.HasComment(
                    "A figure is stored as anchor x,y plus geometry jsonb relative to that anchor: circle {r} " +
                    "about the centre, rectangle/triangle {w,h}, line {dx,dy}. Geometry has no database CHECK; " +
                    "the server is the sole writer and constructs well-formed JSON in code.");
            });

            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();

            entity.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(f => f.Type)
                .HasColumnName("type")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(f => f.X).HasColumnName("x").HasColumnType("integer").IsRequired();
            entity.Property(f => f.Y).HasColumnName("y").HasColumnType("integer").IsRequired();
            entity.Property(f => f.Geometry).HasColumnName("geometry").HasColumnType("jsonb").IsRequired();
            entity.Property(f => f.Z).HasColumnName("z").HasColumnType("numeric").IsRequired();

            entity.HasIndex(f => new { f.UserId, f.Z })
                .HasDatabaseName("ix_figures_user_id_z");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
