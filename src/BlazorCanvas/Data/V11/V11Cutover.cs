using BlazorCanvas.Data.V11.Transition;
using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>Performs the one-way, transactional promotion from the isolated upgrade schema.</summary>
public static class V11Cutover
{
    public static async Task EnsureAsync(NpgsqlDataSource dataSource, ShapeRegistry registry, CancellationToken ct = default)
        => await EnsureAsync(dataSource, registry, failureProbe: null, ct);

    /// <summary>
    /// Test-only transaction probe.  The production overload above deliberately supplies no
    /// probe, so there is no runtime configuration switch or alternate cutover path.
    /// </summary>
    internal static async Task EnsureAsync(
        NpgsqlDataSource dataSource,
        ShapeRegistry registry,
        Func<V11CutoverStage, CancellationToken, Task>? failureProbe,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource); ArgumentNullException.ThrowIfNull(registry);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(11011)", connection, transaction)) await lockCommand.ExecuteNonQueryAsync(ct);
        var state = await GetStateAsync(connection, transaction, ct);
        if (state == CatalogState.Completed)
        {
            await V11Schema.SeedPublicFigureTypesAsync(connection, transaction, registry, ct);
            await transaction.CommitAsync(ct);
            return;
        }
        if (state == CatalogState.Invalid) throw new InvalidOperationException("The v1.11 catalog is partial or unsupported; refusing destructive cutover.");
        if (state is CatalogState.LegacyOnly or CatalogState.Additive)
        {
            await V11Schema.ApplyAsync(connection, transaction, ct);
            await ProbeAsync(V11CutoverStage.AfterSchemaApply, failureProbe, ct);
            await V11Schema.SeedFigureTypesAsync(connection, transaction, registry, ct);
            await ProbeAsync(V11CutoverStage.AfterTypeSeed, failureProbe, ct);
            var report = await V11DataMigration.RunAsync(connection, transaction, registry, ct);
            await ProbeAsync(V11CutoverStage.AfterMigration, failureProbe, ct);
            if (report.FiguresSeen != report.FiguresInserted + report.FiguresAlreadyPresent) throw new InvalidOperationException("The legacy migration report is incomplete.");
        }
        else if (state == CatalogState.FreshUsersOnly)
        {
            await V11Schema.ApplyAsync(connection, transaction, ct);
            await ProbeAsync(V11CutoverStage.AfterSchemaApply, failureProbe, ct);
            await V11Schema.SeedFigureTypesAsync(connection, transaction, registry, ct);
            await ProbeAsync(V11CutoverStage.AfterTypeSeed, failureProbe, ct);
        }
        await using (var drop = new NpgsqlCommand("DROP TABLE IF EXISTS public.figures", connection, transaction)) await drop.ExecuteNonQueryAsync(ct);
        await ProbeAsync(V11CutoverStage.AfterDropPublicFigures, failureProbe, ct);
        foreach (var table in new[] { "canvases", "figure_types", "figures" })
        {
            await using var promote = new NpgsqlCommand($"ALTER TABLE v11.{table} SET SCHEMA public", connection, transaction);
            await promote.ExecuteNonQueryAsync(ct);
            if (table == "canvases") await ProbeAsync(V11CutoverStage.AfterPromoteCanvases, failureProbe, ct);
        }
        await using (var dropSchema = new NpgsqlCommand("DROP SCHEMA v11", connection, transaction)) await dropSchema.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static Task ProbeAsync(V11CutoverStage stage, Func<V11CutoverStage, CancellationToken, Task>? probe, CancellationToken ct) =>
        probe is null ? Task.CompletedTask : probe(stage, ct);

    private enum CatalogState { LegacyOnly, Additive, FreshUsersOnly, Completed, Invalid }
    private static async Task<CatalogState> GetStateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        async Task<bool> Has(string name) { await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection, transaction); command.Parameters.AddWithValue("name", name); return (bool)(await command.ExecuteScalarAsync(ct))!; }
        var users = await Has("public.users"); var legacy = await Has("public.figures"); var vCanvases = await Has("v11.canvases"); var vTypes = await Has("v11.figure_types"); var vFigures = await Has("v11.figures"); var allV11 = vCanvases && vTypes && vFigures;
        var publicCanvases = await Has("public.canvases"); var publicTypes = await Has("public.figure_types"); var publicFigures = legacy; var allPublic = publicCanvases && publicTypes && publicFigures;
        if (!users) return CatalogState.Invalid;
        if (allPublic && !vCanvases && !vTypes && !vFigures) return CatalogState.Completed;
        if (legacy && !vCanvases && !vTypes && !vFigures && !publicCanvases && !publicTypes) return CatalogState.LegacyOnly;
        if (legacy && allV11 && !publicCanvases && !publicTypes) return CatalogState.Additive;
        if (!legacy && !vCanvases && !vTypes && !vFigures && !publicCanvases && !publicTypes) return CatalogState.FreshUsersOnly;
        return CatalogState.Invalid;
    }
}

internal enum V11CutoverStage
{
    AfterSchemaApply,
    AfterTypeSeed,
    AfterMigration,
    AfterDropPublicFigures,
    AfterPromoteCanvases,
}
