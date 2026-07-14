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
                // circle: stored as the square it is inscribed in (D-22)
                t.HasCheckConstraint(
                    "circle_is_a_circle",
                    "type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0)");

                // rectangle / triangle: a real box, normalised, no zero width or height (D-23, D-41)
                t.HasCheckConstraint(
                    "box_is_a_box",
                    "type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1)");

                // line: normalised left-to-right; may run either way vertically; never zero-length
                t.HasCheckConstraint(
                    "line_is_a_line",
                    "type <> 'line' OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1))");

                // type must be one of the four known lowercase literals (D-46)
                t.HasCheckConstraint(
                    "figures_type_is_known",
                    "type IN ('line','rectangle','circle','triangle')");

                t.HasComment(
                    "x1,y1,x2,y2 are ALWAYS the figure's bounding box. A CIRCLE is stored as the square it is " +
                    "inscribed in: r = (x2-x1)/2, cx = x1+r, cy = y1+r. It is DRAWN centre-out (press centre, " +
                    "drag for radius) but STORED as a square — interaction and storage are different things. " +
                    "A LINE is the segment between the two points and may run diagonally in either vertical " +
                    "direction; it is normalised by swapping the whole point pair, never by sorting axes.");
            });

            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(f => f.Type)
                .HasColumnName("type")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(f => f.X1).HasColumnName("x1").IsRequired();
            entity.Property(f => f.Y1).HasColumnName("y1").IsRequired();
            entity.Property(f => f.X2).HasColumnName("x2").IsRequired();
            entity.Property(f => f.Y2).HasColumnName("y2").IsRequired();

            entity.HasIndex(f => f.UserId)
                .HasDatabaseName("ix_figures_user_id");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
