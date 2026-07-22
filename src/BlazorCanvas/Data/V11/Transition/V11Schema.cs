using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11.Transition;

/// <summary>Temporary v1.11 schema used only while upgrading a legacy catalog.</summary>
public static class V11Schema
{
    public const string SchemaName = "v11";
    public const string Ddl = """
        CREATE SCHEMA IF NOT EXISTS v11;
        CREATE TABLE IF NOT EXISTS v11.canvases (
            id uuid PRIMARY KEY, owner_id integer NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
            name text NOT NULL DEFAULT 'Canvas', width integer NOT NULL DEFAULT 1472,
            height integer NOT NULL DEFAULT 828, background text NOT NULL DEFAULT '#FFFFFF',
            created_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS ix_canvases_owner ON v11.canvases (owner_id);
        CREATE TABLE IF NOT EXISTS v11.figure_types (name text PRIMARY KEY);
        CREATE TABLE IF NOT EXISTS v11.figures (
            id uuid PRIMARY KEY, canvas_id uuid NOT NULL REFERENCES v11.canvases(id) ON DELETE CASCADE,
            type text NOT NULL REFERENCES v11.figure_types(name), x numeric(12,3) NOT NULL,
            y numeric(12,3) NOT NULL, rotation numeric(7,3) NOT NULL DEFAULT 0,
            geometry jsonb NOT NULL, style jsonb NOT NULL DEFAULT '{}', z numeric NOT NULL,
            bbox_x double precision NOT NULL, bbox_y double precision NOT NULL,
            bbox_w double precision NOT NULL, bbox_h double precision NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            CONSTRAINT z_unique_per_canvas UNIQUE (canvas_id, z),
            CONSTRAINT style_is_object CHECK (jsonb_typeof(style) = 'object'),
            CONSTRAINT geometry_is_object CHECK (jsonb_typeof(geometry) = 'object'),
            CONSTRAINT bbox_is_positive CHECK (bbox_w >= 0 AND bbox_h >= 0));
        CREATE INDEX IF NOT EXISTS ix_figures_canvas_z ON v11.figures (canvas_id, z);
        """;

    public static async Task ApplyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct = default)
    {
        await using var command = new NpgsqlCommand(Ddl, connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task SeedFigureTypesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, ShapeRegistry registry, CancellationToken ct = default)
    {
        foreach (var name in registry.Names)
        {
            await using var command = new NpgsqlCommand("INSERT INTO v11.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING", connection, transaction);
            command.Parameters.AddWithValue("name", name);
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
