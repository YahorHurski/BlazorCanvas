using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// Counts the v1.1 rows seen and the v1.11 rows created by one migration run. Callers may assert
/// that <see cref="FiguresSeen"/> equals <see cref="FiguresInserted"/> plus
/// <see cref="FiguresAlreadyPresent"/>: any discrepancy would mean a legacy figure went missing.
/// </summary>
public sealed record V11MigrationReport(
    int UsersSeen,
    int CanvasesCreated,
    int FiguresSeen,
    int FiguresInserted,
    int FiguresAlreadyPresent);

/// <summary>
/// Migrates the additive v1.11 data model. The draft's destructive fourth step is deliberately
/// deferred to Phase 11; this code reads public tables but never alters them. Migrated created_at
/// values are migration timestamps, not the unavailable original creation time (D-68).
/// </summary>
public static class V11DataMigration
{
    private const string CanvasInsertSql = """
        INSERT INTO v11.canvases (id, owner_id, name, width, height, background)
        VALUES (@id, @owner_id, 'Canvas', 1472, 828, '#FFFFFF')
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>
    /// Opens a short-lived connection and delegates to the connection overload.
    /// </summary>
    public static async Task<V11MigrationReport> RunAsync(
        NpgsqlDataSource dataSource,
        ShapeRegistry registry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await RunAsync(connection, registry, ct);
    }

    /// <summary>
    /// Runs against an already-open connection, allowing the replay harness to target only its own
    /// guarded scratch database.
    /// </summary>
    public static async Task<V11MigrationReport> RunAsync(
        NpgsqlConnection connection,
        ShapeRegistry registry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(registry);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("The migration requires an open connection.");
        }

        await using var transaction = await connection.BeginTransactionAsync(ct);
        // PostgreSQL DDL is transactional. Apply the additive schema and registry seed inside this
        // same migration transaction so an invalid legacy row leaves a legacy-only database.
        await V11Schema.ApplyAsync(connection, ct, transaction);
        await V11Schema.SeedFigureTypesAsync(connection, registry, ct, transaction);

        await using var repositoryDataSource = NpgsqlDataSource.Create(connection.ConnectionString);
        var repository = new FigureRepository(repositoryDataSource);
        var gateway = new FigureInputGateway(registry);

        var users = await ReadUserIdsAsync(connection, transaction, ct);
            var canvasesCreated = 0;
            foreach (var userId in users)
            {
                // Every user, including figure-less users, gets a canvas; omitting them would make
                // an unapproved product decision. The fixture has 173 such users.
                await using var command = new NpgsqlCommand(CanvasInsertSql, connection, transaction);
                command.Parameters.AddWithValue("id", V11DeterministicId.ForCanvas(userId));
                command.Parameters.AddWithValue("owner_id", userId);
                canvasesCreated += await command.ExecuteNonQueryAsync(ct);
            }

            // Read before writing so a legacy reader is never held open across v11 inserts.
            var figures = await ReadLegacyFiguresAsync(connection, transaction, ct);
            var figuresInserted = 0;
            var figuresAlreadyPresent = 0;
            foreach (var row in figures)
            {
                var converted = LegacyFigureConversion.Convert(row);
                var definition = registry.Get(row.Type);
                var geometryJson = definition.ToJson(converted.Geometry);

                // The JSON round-trip is intentional: Phase 9's gateway remains the one validation
                // boundary for every write. A null style makes StyleGateway supply today's fixed style.
                if (!gateway.TryValidate(row.Type, geometryJson, null, out var input))
                {
                    throw new InvalidOperationException(
                        $"Legacy figure {row.Id} of type '{row.Type}' could not be validated.");
                }

                // The repository owns the only bbox-writing insert. z is the old id verbatim:
                // v1.1 ordered by id, and one canvas per user makes it unique within each canvas.
                var inserted = await repository.InsertWithIdAndZAsync(
                    connection,
                    transaction,
                    V11DeterministicId.ForFigure(row.Id),
                    V11DeterministicId.ForCanvas(row.UserId),
                    input!,
                    converted.X,
                    converted.Y,
                    row.Id,
                    rotation: 0m,
                    ct);
                if (inserted)
                {
                    figuresInserted++;
                }
                else
                {
                    figuresAlreadyPresent++;
                }
            }

            if (figures.Count != figuresInserted + figuresAlreadyPresent)
            {
                throw new InvalidOperationException("The migration report would omit one or more legacy figures.");
            }

            await transaction.CommitAsync(ct);
        return new V11MigrationReport(
            users.Count,
            canvasesCreated,
            figures.Count,
            figuresInserted,
            figuresAlreadyPresent);
    }

    private static async Task<List<int>> ReadUserIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT id FROM public.users ORDER BY id", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var users = new List<int>();
        while (await reader.ReadAsync(ct))
        {
            users.Add(reader.GetInt32(0));
        }

        return users;
    }

    private static async Task<List<LegacyFigureRow>> ReadLegacyFiguresAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        const string sql = "SELECT id, user_id, type, x1, y1, x2, y2 FROM public.figures ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var figures = new List<LegacyFigureRow>();
        while (await reader.ReadAsync(ct))
        {
            figures.Add(new LegacyFigureRow(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3),
                reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        }

        return figures;
    }
}
