using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Data;

/// <summary>
/// The app's only figure load/insert path. "The canvas" is not a row or an entity — it is the
/// `WHERE user_id = @id ORDER BY id` query in <see cref="LoadAsync"/> (D-03, D-12). Every method
/// takes its own short-lived <see cref="CanvasDbContext"/> from the runtime
/// <see cref="IDbContextFactory{TContext}"/> (Task 1), never a long-lived one, so this store is
/// safe to call repeatedly from a long-lived InteractiveServer circuit.
///
/// This store never normalises, clamps, or guards geometry, and it never discovers whose canvas
/// it is on its own — `userId` is always a parameter, supplied by the caller from the `user_id`
/// cookie claim (T-03-01). It also never updates or deletes; those are Phase 4 (FIG-03, FIG-04).
/// </summary>
public sealed class FigureStore(IDbContextFactory<CanvasDbContext> factory)
{
    /// <summary>
    /// Loads a user's whole canvas: every figure they own, in creation order. `OrderBy(f => f.Id)`
    /// is not cosmetic — `figures.id` IS the z-order (D-39), SVG paints in document order, and
    /// this ordering is the only thing that makes overlap/occlusion identical after an F5. There
    /// is no `created_at` column to order by instead (D-46).
    /// </summary>
    public async Task<List<Figure>> LoadAsync(int userId)
    {
        await using var db = await factory.CreateDbContextAsync();

        return await db.Figures
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts one figure and returns it AFTER `SaveChangesAsync`, so EF Core has populated
    /// `Id` from the database identity column. This ordering is D-39 and it is load-bearing for
    /// Phase 5: the id does not exist until the INSERT completes, so the `draw` broadcast can
    /// never be fired optimistically.
    /// </summary>
    public async Task<Figure> InsertAsync(int userId, FigureType type, Box box)
    {
        await using var db = await factory.CreateDbContextAsync();

        var figure = new Figure
        {
            UserId = userId,
            Type = FigureTypeNames.ToDbValue(type),
            X1 = box.X1,
            Y1 = box.Y1,
            X2 = box.X2,
            Y2 = box.Y2,
        };

        db.Figures.Add(figure);
        await db.SaveChangesAsync();

        return figure;
    }
}
