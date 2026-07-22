using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// Prepares the additive v1.11 store before any interactive circuit can resolve a canvas. This is
/// deliberately a startup coordinator, not request middleware: migration is bounded to process
/// startup and failures prevent the application from serving a mixed persistence model.
/// </summary>
public static class V11RuntimeBootstrap
{
    /// <summary>
    /// Applies and seeds the v1.11 schema. When the legacy figures table is still present, replays
    /// it through the Phase 10 migration; the migration's deterministic ids make later startups
    /// verifiable no-op reruns without dropping the v1.1 safety net.
    /// </summary>
    public static async Task EnsureAsync(
        NpgsqlDataSource dataSource,
        ShapeRegistry registry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(registry);

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        if (!await HasTableAsync(connection, "public.users", ct))
        {
            throw new InvalidOperationException("The v1.11 bootstrap requires the EF users schema before it can run.");
        }

        if (await HasTableAsync(connection, "public.figures", ct))
        {
            // V11DataMigration owns the single transactional apply/seed/replay path. It reports
            // every legacy figure as either inserted or already present, so a restart cannot
            // silently discard a row while public.figures remains available for recovery.
            await V11DataMigration.RunAsync(connection, registry, ct);
        }
        else
        {
            await V11Schema.ApplyAsync(connection, ct);
            await V11Schema.SeedFigureTypesAsync(connection, registry, ct);
        }

        await VerifyCatalogAsync(connection, registry, ct);
    }

    private static async Task<bool> HasTableAsync(NpgsqlConnection connection, string qualifiedName, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", qualifiedName);
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    private static async Task VerifyCatalogAsync(NpgsqlConnection connection, ShapeRegistry registry, CancellationToken ct)
    {
        foreach (var table in new[] { "v11.canvases", "v11.figure_types", "v11.figures" })
        {
            if (!await HasTableAsync(connection, table, ct))
            {
                throw new InvalidOperationException($"The v1.11 bootstrap left required table '{table}' unavailable.");
            }
        }

        const string countSql = "SELECT count(*) FROM v11.figure_types WHERE name = ANY(@names)";
        await using var command = new NpgsqlCommand(countSql, connection);
        command.Parameters.AddWithValue("names", registry.Names.ToArray());
        var seeded = (long)(await command.ExecuteScalarAsync(ct))!;
        if (seeded != registry.Names.Count)
        {
            throw new InvalidOperationException("The v1.11 figure-type catalog is only partially seeded.");
        }
    }
}
