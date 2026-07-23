using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Data;

/// <summary>
/// The app's only figure load/insert/update/delete path. "The canvas" is not a row or an entity — it is the
/// `WHERE user_id = @id ORDER BY z, id` query in <see cref="LoadAsync"/> (D-03, D-12). Every method
/// takes its own short-lived <see cref="CanvasDbContext"/> from the runtime
/// <see cref="IDbContextFactory{TContext}"/> (Task 1), never a long-lived one, so this store is
/// safe to call repeatedly from a long-lived InteractiveServer circuit.
///
/// This store never normalises, clamps, or guards geometry, and it never discovers whose canvas
/// it is on its own — `userId` is always a parameter, supplied by the caller from the `user_id`
/// cookie claim (T-03-01). It owns all three persistence verbs while geometry rules stay with
/// the caller and the database constraints.
/// </summary>
public sealed class FigureStore(IDbContextFactory<CanvasDbContext> factory)
{
    /// <summary>
    /// Loads a user's whole canvas: every figure they own, in z-order. SVG paints in document
    /// order, so z is the persisted layer order while id is the stable tiebreak.
    /// </summary>
    public async Task<List<Figure>> LoadAsync(int userId)
    {
        await using var db = await factory.CreateDbContextAsync();

        return await db.Figures
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Z)
            .ThenBy(f => f.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts one figure and returns it after saving, so EF Core has populated
    /// `Id` from the database identity column. This ordering is D-39 and it is load-bearing for
    /// Phase 5: the id does not exist until the INSERT completes, so the `draw` broadcast can
    /// never be fired optimistically.
    /// </summary>
    public async Task<Figure> InsertAsync(int userId, FigureType type, Box box)
    {
        await using var db = await factory.CreateDbContextAsync();

        var encoded = GeometryCodec.Encode(type, box);
        var nextZ = ((await db.Figures
            .Where(f => f.UserId == userId)
            .MaxAsync(f => (decimal?)f.Z)) ?? 0m) + 1m;

        var figure = new Figure
        {
            UserId = userId,
            Type = FigureTypeNames.ToDbValue(type),
            X = encoded.X,
            Y = encoded.Y,
            Geometry = encoded.Geometry,
            Z = nextZ,
        };

        db.Figures.Add(figure);
        await db.SaveChangesAsync();

        return figure;
    }

    /// <summary>
    /// Persists the single database write made by a whole drag: one UPDATE on drop, never on
    /// pointer-move (D-09). The affected-row count is D-10's staleness guard: 0 means the
    /// figure is already gone, not that this call failed. The `userId` term in the filter is
    /// the IDOR guard, so a caller can only move figures from their own canvas.
    /// </summary>
    public async Task<int> UpdateAsync(int userId, Guid figureId, Box box)
    {
        await using var db = await factory.CreateDbContextAsync();
        var typeValue = await db.Figures
            .Where(f => f.Id == figureId && f.UserId == userId)
            .Select(f => f.Type)
            .FirstOrDefaultAsync();

        if (typeValue is null)
        {
            return 0;
        }

        var encoded = GeometryCodec.Encode(FigureTypeNames.Parse(typeValue), box);

        return await db.Figures
            .Where(f => f.Id == figureId && f.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.X, encoded.X)
                .SetProperty(f => f.Y, encoded.Y)
                .SetProperty(f => f.Geometry, encoded.Geometry));
    }

    /// <summary>
    /// Deletes one owned figure and returns the affected-row count. Delete-of-a-ghost is
    /// naturally idempotent (D-10 says only UPDATE needs the guard), so the count is returned
    /// for symmetry with <see cref="UpdateAsync"/> and for tests rather than because the caller
    /// must branch on it.
    /// </summary>
    public async Task<int> DeleteAsync(int userId, Guid figureId)
    {
        await using var db = await factory.CreateDbContextAsync();

        return await db.Figures
            .Where(f => f.Id == figureId && f.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
