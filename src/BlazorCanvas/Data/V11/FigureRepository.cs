using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// The final v1.11 persistence surface. Every geometry/style write takes <see cref="ValidatedFigureInput"/>,
/// so client data cannot bypass <see cref="FigureInputGateway"/>.
/// </summary>
public sealed class FigureRepository(NpgsqlDataSource dataSource)
{
    private const int InsertRetryAttempts = 5;
    private const string ZUniqueConstraint = "z_unique_per_canvas";

    private const string LoadSql = """
        SELECT id, canvas_id, type, x, y, rotation, geometry::text, style::text, z, bbox_x, bbox_y, bbox_w, bbox_h
        FROM public.figures
        WHERE canvas_id = @canvas_id
        ORDER BY z
        """;

    // D-63 makes z the SVG stacking order. UNIQUE (canvas_id, z) makes this ordering total.
    private const string AutoInsertSql = """
        INSERT INTO public.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h)
        VALUES (@id, @canvas_id, @type, @x, @y, @rotation, @geometry::jsonb, @style::jsonb,
            COALESCE((SELECT MAX(z) FROM public.figures WHERE canvas_id = @canvas_id), 0) + 1,
            @bbox_x, @bbox_y, @bbox_w, @bbox_h)
        RETURNING id, canvas_id, type, x, y, rotation, geometry::text, style::text, z, bbox_x, bbox_y, bbox_w, bbox_h
        """;

    private const string ExplicitInsertSql = """
        INSERT INTO public.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h)
        VALUES (@id, @canvas_id, @type, @x, @y, @rotation, @geometry::jsonb, @style::jsonb, @z,
            @bbox_x, @bbox_y, @bbox_w, @bbox_h)
        ON CONFLICT (id) DO NOTHING
        RETURNING id, canvas_id, type, x, y, rotation, geometry::text, style::text, z, bbox_x, bbox_y, bbox_w, bbox_h
        """;

    // MODEL-01 is literal here: a move writes exactly these two local-frame-independent columns.
    private const string MoveSql = "UPDATE public.figures SET x = @x, y = @y WHERE id = @id AND canvas_id = @canvas_id";
    private const string DeleteSql = "DELETE FROM public.figures WHERE id = @id AND canvas_id = @canvas_id";

    /// <summary>
    /// Loads one owned canvas in ascending z order. SVG paints in document order, so ordering is
    /// observable stacking behaviour rather than a display convenience.
    /// </summary>
    public async Task<IReadOnlyList<FigureRow>> LoadAsync(Guid canvasId, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(LoadSql, connection);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var figures = new List<FigureRow>();
        while (await reader.ReadAsync(ct))
        {
            figures.Add(ReadRow(reader));
        }

        return figures;
    }

    /// <summary>
    /// Creates a draw event with a UUID before its INSERT is issued. D-63 requires a bounded retry
    /// because otherwise a concurrent z collision makes a figure silently fail to appear. Each retry
    /// recomputes MAX(z), filters solely on z_unique_per_canvas (never a primary-key error), and is
    /// distinct from Program.cs's transport-failure retry policy.
    /// </summary>
    public async Task<FigureRow> InsertAsync(
        Guid canvasId,
        ValidatedFigureInput input,
        decimal x,
        decimal y,
        decimal rotation = 0m,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var id = Guid.NewGuid();

        for (var attempt = 1; attempt <= InsertRetryAttempts; attempt++)
        {
            try
            {
                var row = await InsertCoreAsync(AutoInsertSql, id, canvasId, input, x, y, rotation, null, ct);
                return row ?? throw new InvalidOperationException("The draw INSERT did not return a row.");
            }
            catch (PostgresException ex) when (attempt < InsertRetryAttempts
                && ex.SqlState == PostgresErrorCodes.UniqueViolation
                && ex.ConstraintName == ZUniqueConstraint)
            {
                // A fresh command and data-source connection on the next iteration recompute MAX(z).
            }
        }

        throw new InvalidOperationException("The insert retry loop exited unexpectedly.");
    }

    /// <summary>
    /// Inserts a migration row with caller-supplied identity and layer. Duplicate identities are
    /// idempotent no-ops; a z collision deliberately propagates because relocating legacy content
    /// would silently change its stacking order.
    /// </summary>
    public async Task<bool> InsertWithIdAndZAsync(
        Guid id,
        Guid canvasId,
        ValidatedFigureInput input,
        decimal x,
        decimal y,
        decimal z,
        decimal rotation = 0m,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await InsertCoreAsync(ExplicitInsertSql, id, canvasId, input, x, y, rotation, z, ct) is not null;
    }


    /// <summary>
    /// Moves an owned figure with one UPDATE and no geometry read or type dispatch. Its affected-row
    /// count is D-10's staleness guard; zero means gone, and never inserting here preserves D-40.
    /// The local bbox cache therefore remains valid without appearing in this SET list.
    /// </summary>
    public async Task<int> MoveAsync(Guid canvasId, Guid figureId, decimal x, decimal y, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(MoveSql, connection);
        command.Parameters.AddWithValue("x", x);
        command.Parameters.AddWithValue("y", y);
        command.Parameters.AddWithValue("id", figureId);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        return await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Hard-deletes one owned figure and returns its affected-row count. The canvas predicate is the
    /// ownership guard even when a predictable migrated UUID is supplied by another caller.
    /// </summary>
    public async Task<int> DeleteAsync(Guid canvasId, Guid figureId, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(DeleteSql, connection);
        command.Parameters.AddWithValue("id", figureId);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        return await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// D-67 and MODEL-07 require exactly one cache-writing path: all bbox values come verbatim from
    /// ValidatedFigureInput.Bounds, the registry's local geometry calculation. A second INSERT would
    /// reopen the cache-staleness hole this boundary closes.
    /// </summary>
    private async Task<FigureRow?> InsertCoreAsync(
        string sql,
        Guid id,
        Guid canvasId,
        ValidatedFigureInput input,
        decimal x,
        decimal y,
        decimal rotation,
        decimal? z,
        CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        AddInsertParameters(command, id, canvasId, input, x, y, rotation, z);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadRow(reader) : null;
    }


    private static void AddInsertParameters(
        NpgsqlCommand command,
        Guid id,
        Guid canvasId,
        ValidatedFigureInput input,
        decimal x,
        decimal y,
        decimal rotation,
        decimal? z)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        command.Parameters.AddWithValue("type", input.Type);
        command.Parameters.AddWithValue("x", x);
        command.Parameters.AddWithValue("y", y);
        command.Parameters.AddWithValue("rotation", rotation);
        command.Parameters.AddWithValue("geometry", input.GeometryJson);
        command.Parameters.AddWithValue("style", input.StyleJson);
        command.Parameters.AddWithValue("bbox_x", input.Bounds.X);
        command.Parameters.AddWithValue("bbox_y", input.Bounds.Y);
        command.Parameters.AddWithValue("bbox_w", input.Bounds.W);
        command.Parameters.AddWithValue("bbox_h", input.Bounds.H);
        if (z is decimal explicitZ)
        {
            command.Parameters.AddWithValue("z", explicitZ);
        }
    }

    private static FigureRow ReadRow(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetFieldValue<decimal>(3),
        reader.GetFieldValue<decimal>(4),
        reader.GetFieldValue<decimal>(5),
        reader.GetString(6),
        reader.GetString(7),
        reader.GetFieldValue<decimal>(8),
        reader.GetDouble(9),
        reader.GetDouble(10),
        reader.GetDouble(11),
        reader.GetDouble(12));
}
