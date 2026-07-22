using BlazorCanvas.Shapes;
using BlazorCanvas.Data.V11;
using Npgsql;

namespace BlazorCanvas.Data.V11.Transition;

public sealed record V11MigrationReport(int UsersSeen, int CanvasesCreated, int FiguresSeen, int FiguresInserted, int FiguresAlreadyPresent);

/// <summary>Legacy replay. The connection overload participates in a transaction owned by V11Cutover.</summary>
public static class V11DataMigration
{
    private const string CanvasInsertSql = "INSERT INTO v11.canvases (id, owner_id, name, width, height, background) VALUES (@id, @owner_id, 'Canvas', 1472, 828, '#FFFFFF') ON CONFLICT (id) DO NOTHING";

    public static async Task<V11MigrationReport> RunAsync(NpgsqlDataSource dataSource, ShapeRegistry registry, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await V11Schema.ApplyAsync(connection, transaction, ct);
        await V11Schema.SeedFigureTypesAsync(connection, transaction, registry, ct);
        var report = await RunAsync(connection, transaction, registry, ct);
        await transaction.CommitAsync(ct);
        return report;
    }

    public static async Task<V11MigrationReport> RunAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, ShapeRegistry registry, CancellationToken ct = default)
    {
        if (connection.State != System.Data.ConnectionState.Open) throw new InvalidOperationException("The migration requires an open connection.");
        var users = new List<int>();
        await using (var command = new NpgsqlCommand("SELECT id FROM public.users ORDER BY id", connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync(ct)) while (await reader.ReadAsync(ct)) users.Add(reader.GetInt32(0));
        var canvasesCreated = 0;
        foreach (var userId in users) { await using var command = new NpgsqlCommand(CanvasInsertSql, connection, transaction); command.Parameters.AddWithValue("id", V11DeterministicId.ForCanvas(userId)); command.Parameters.AddWithValue("owner_id", userId); canvasesCreated += await command.ExecuteNonQueryAsync(ct); }
        var rows = new List<LegacyFigureRow>();
        await using (var command = new NpgsqlCommand("SELECT id, user_id, type, x1, y1, x2, y2 FROM public.figures ORDER BY id", connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync(ct)) while (await reader.ReadAsync(ct)) rows.Add(new LegacyFigureRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        var writer = new V11TransitionFigureWriter(connection, transaction); var gateway = new FigureInputGateway(registry); var inserted = 0;
        foreach (var row in rows) { var converted = LegacyFigureConversion.Convert(row); var json = registry.Get(row.Type).ToJson(converted.Geometry); if (!gateway.TryValidate(row.Type, json, null, out var input)) throw new InvalidOperationException("A legacy figure could not be validated."); if (await writer.InsertAsync(V11DeterministicId.ForFigure(row.Id), V11DeterministicId.ForCanvas(row.UserId), input!, converted.X, converted.Y, row.Id, ct)) inserted++; }
        return new V11MigrationReport(users.Count, canvasesCreated, rows.Count, inserted, rows.Count - inserted);
    }
}
