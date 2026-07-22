using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// Defines the additive v1.11 storage schema and seeds its shape-type lookup table.
/// </summary>
public static class V11Schema
{
    // Phase 10 keeps the new tables isolated so their exact names can coexist with public.figures.
    // Phase 11 cuts over with DROP TABLE public.figures, ALTER TABLE v11.<t> SET SCHEMA public for
    // each new table, then DROP SCHEMA v11.
    public const string SchemaName = "v11";

    // This constant is the single storage-model definition. There is intentionally no mirrored EF
    // entity model: duplicating a rule in two places was the D-50 problem that D-67 removes.
    // It never changes search_path or writes to an existing public table.
    public const string Ddl = """
        CREATE SCHEMA IF NOT EXISTS v11;

        CREATE TABLE IF NOT EXISTS v11.canvases (
            id uuid PRIMARY KEY,
            owner_id integer NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
            name text NOT NULL DEFAULT 'Canvas',
            width integer NOT NULL DEFAULT 1472,
            height integer NOT NULL DEFAULT 828,
            background text NOT NULL DEFAULT '#FFFFFF',
            created_at timestamptz NOT NULL DEFAULT now()
        );

        -- D-64 keeps multiple canvases available later; one canvas per owner is a migration invariant.
        CREATE INDEX IF NOT EXISTS ix_canvases_owner ON v11.canvases (owner_id);

        CREATE TABLE IF NOT EXISTS v11.figure_types (
            name text PRIMARY KEY
        );

        CREATE TABLE IF NOT EXISTS v11.figures (
            id uuid PRIMARY KEY,
            canvas_id uuid NOT NULL REFERENCES v11.canvases(id) ON DELETE CASCADE,
            type text NOT NULL REFERENCES v11.figure_types(name),
            x numeric(12,3) NOT NULL,
            y numeric(12,3) NOT NULL,
            rotation numeric(7,3) NOT NULL DEFAULT 0,
            geometry jsonb NOT NULL,
            style jsonb NOT NULL DEFAULT '{}',
            z numeric NOT NULL,
            bbox_x double precision NOT NULL,
            bbox_y double precision NOT NULL,
            bbox_w double precision NOT NULL,
            bbox_h double precision NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            CONSTRAINT z_unique_per_canvas UNIQUE (canvas_id, z),
            CONSTRAINT style_is_object CHECK (jsonb_typeof(style) = 'object'),
            CONSTRAINT geometry_is_object CHECK (jsonb_typeof(geometry) = 'object'),
            -- >= 0 deliberately permits horizontal and vertical lines.
            CONSTRAINT bbox_is_positive CHECK (bbox_w >= 0 AND bbox_h >= 0)
        );

        CREATE INDEX IF NOT EXISTS ix_figures_canvas_z ON v11.figures (canvas_id, z);

        COMMENT ON TABLE v11.figures IS
            'x, y, rotation locate the figure; geometry is local shape data from (0,0); bbox_* is a local, stroke-excluding cache of geometry alone. A move writes x and y only.';
        """;

    /// <summary>
    /// Opens a connection and applies the idempotent DDL batch.
    /// </summary>
    public static async Task ApplyAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await ApplyAsync(connection, ct);
    }

    /// <summary>
    /// Applies the idempotent DDL batch through an already-open connection. Supplying a transaction
    /// keeps PostgreSQL's transactional DDL in the caller's rollback boundary.
    /// </summary>
    public static async Task ApplyAsync(
        NpgsqlConnection connection,
        CancellationToken ct = default,
        NpgsqlTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await using var command = new NpgsqlCommand(Ddl, connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Seeds exact registry names in registration order. The conflict clause lets simultaneous
    /// application instances make duplicate seeding a no-op; a new shape costs this row and its class.
    /// </summary>
    public static async Task SeedFigureTypesAsync(
        NpgsqlConnection connection,
        ShapeRegistry registry,
        CancellationToken ct = default,
        NpgsqlTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var name in registry.Names)
        {
            // DDL is constant; this caller-provided value is always a bound NpgsqlParameter.
            await using var command = new NpgsqlCommand(
                "INSERT INTO v11.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING",
                connection,
                transaction);
            command.Parameters.Add(new NpgsqlParameter("name", name));
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    /// <summary>
    /// Applies the storage schema, then seeds the supplied canonical registry.
    /// </summary>
    public static async Task ApplyAndSeedAsync(
        string connectionString,
        ShapeRegistry registry,
        CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await ApplyAsync(connection, ct);
        await SeedFigureTypesAsync(connection, registry, ct);
    }
}
