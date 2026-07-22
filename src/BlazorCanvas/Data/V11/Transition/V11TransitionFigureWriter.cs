using Npgsql;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Data.V11.Transition;

/// <summary>Transaction-bound writer for the temporary schema. Runtime repositories never use it.</summary>
internal sealed class V11TransitionFigureWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    private const string Sql = """
        INSERT INTO v11.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h)
        VALUES (@id, @canvas_id, @type, @x, @y, @rotation, @geometry::jsonb, @style::jsonb, @z, @bbox_x, @bbox_y, @bbox_w, @bbox_h)
        ON CONFLICT (id) DO NOTHING
        """;

    public async Task<bool> InsertAsync(Guid id, Guid canvasId, ValidatedFigureInput input, decimal x, decimal y, decimal z, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(Sql, connection, transaction);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("canvas_id", canvasId);
        command.Parameters.AddWithValue("type", input.Type); command.Parameters.AddWithValue("x", x);
        command.Parameters.AddWithValue("y", y); command.Parameters.AddWithValue("rotation", 0m);
        command.Parameters.AddWithValue("geometry", input.GeometryJson); command.Parameters.AddWithValue("style", input.StyleJson);
        command.Parameters.AddWithValue("z", z); command.Parameters.AddWithValue("bbox_x", input.Bounds.X);
        command.Parameters.AddWithValue("bbox_y", input.Bounds.Y); command.Parameters.AddWithValue("bbox_w", input.Bounds.W);
        command.Parameters.AddWithValue("bbox_h", input.Bounds.H);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
}
