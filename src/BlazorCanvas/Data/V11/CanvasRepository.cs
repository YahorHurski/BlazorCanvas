using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// The one v1.11 canvas belonging to an authenticated legacy owner. The id is deterministic so it
/// remains stable across migration, restarts, and independent application instances.
/// </summary>
public sealed record CanvasRow(
    Guid Id,
    int OwnerId,
    int Width,
    int Height,
    string Name,
    string Background);

/// <summary>
/// Resolves a canvas from its authenticated owner id. Callers never supply a canvas id, so a
/// predictable migrated UUID cannot be used to cross an ownership boundary.
/// </summary>
public sealed class CanvasRepository(NpgsqlDataSource dataSource)
{
    private const string EnsureSql = """
        INSERT INTO public.canvases (id, owner_id, name, width, height, background)
        VALUES (@id, @owner_id, 'Canvas', 1472, 828, '#FFFFFF')
        ON CONFLICT (id) DO NOTHING
        """;

    private const string LoadSql = """
        SELECT id, owner_id, width, height, name, background
        FROM public.canvases
        WHERE id = @id AND owner_id = @owner_id
        """;

    /// <summary>
    /// Ensures and returns the caller's deterministic canvas. A missing user is intentionally not
    /// translated: the users foreign-key failure makes an invalid authenticated owner visible.
    /// </summary>
    public async Task<CanvasRow> EnsureForOwnerAsync(int ownerId, CancellationToken ct = default)
    {
        var id = V11DeterministicId.ForCanvas(ownerId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        await using (var ensure = new NpgsqlCommand(EnsureSql, connection))
        {
            ensure.Parameters.AddWithValue("id", id);
            ensure.Parameters.AddWithValue("owner_id", ownerId);
            await ensure.ExecuteNonQueryAsync(ct);
        }

        await using var load = new NpgsqlCommand(LoadSql, connection);
        load.Parameters.AddWithValue("id", id);
        load.Parameters.AddWithValue("owner_id", ownerId);
        await using var reader = await load.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("The ensured canvas could not be read for its owner.");
        }

        return new CanvasRow(
            reader.GetGuid(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetString(4),
            reader.GetString(5));
    }
}
