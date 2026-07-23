using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// Asserts the LIVE schema — queried from PostgreSQL's own information_schema and pg_catalog,
/// never from the EF model — is exactly the D-59 shape (D-12, D-46, D-42). The EF model is the thing under test here; asserting against it would prove
/// nothing about what plan 01-03's migration actually did to the live database.
/// </summary>
[Collection("Database")]
public class SchemaShapeTests
{
    private readonly DatabaseFixture _fixture;

    public SchemaShapeTests(DatabaseFixture fixture) => _fixture = fixture;

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task<List<string>> QueryStringsAsync(NpgsqlConnection conn, string sql)
    {
        var results = new List<string>();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    [Fact]
    public async Task PublicSchema_ContainsExactlyTheThreeExpectedTables_NoCanvasesTable()
    {
        await using var conn = await OpenConnectionAsync();
        var tables = await QueryStringsAsync(
            conn,
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name");

        Assert.Equal(
            new[] { "__EFMigrationsHistory", "figures", "users" }.OrderBy(x => x, StringComparer.Ordinal),
            tables.OrderBy(x => x, StringComparer.Ordinal));
        Assert.DoesNotContain("canvases", tables);
    }

    [Fact]
    public async Task Figures_HasExactlySevenColumns_InOrder()
    {
        await using var conn = await OpenConnectionAsync();
        var columns = await QueryStringsAsync(
            conn,
            "SELECT column_name FROM information_schema.columns " +
            "WHERE table_schema='public' AND table_name='figures'");

        Assert.Equal(
            new[] { "id", "user_id", "type", "x", "y", "geometry", "z" }.OrderBy(x => x, StringComparer.Ordinal),
            columns.OrderBy(x => x, StringComparer.Ordinal));
        Assert.DoesNotContain("created_at", columns);
    }

    [Fact]
    public async Task Users_HasExactlyThreeColumns()
    {
        await using var conn = await OpenConnectionAsync();
        var columns = await QueryStringsAsync(
            conn,
            "SELECT column_name FROM information_schema.columns " +
            "WHERE table_schema='public' AND table_name='users' ORDER BY ordinal_position");

        Assert.Equal(new[] { "id", "username", "password" }, columns);
    }

    [Fact]
    public async Task FiguresType_IsTextColumn_AndNoPostgresEnumTypesExist()
    {
        await using var conn = await OpenConnectionAsync();

        await using (var cmd = new NpgsqlCommand(
            "SELECT data_type FROM information_schema.columns " +
            "WHERE table_schema='public' AND table_name='figures' AND column_name='type'", conn))
        {
            var dataType = (string?)await cmd.ExecuteScalarAsync();
            Assert.Equal("text", dataType);
        }

        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM pg_type WHERE typtype = 'e'", conn))
        {
            var enumCount = (long)(await cmd.ExecuteScalarAsync())!;
            Assert.Equal(0, enumCount);
        }
    }

    [Fact]
    public async Task FiguresId_IsUuidWithDefault_AndUsersId_IsIdentityColumn()
    {
        await using var conn = await OpenConnectionAsync();

        async Task<string?> IsIdentityAsync(string table)
        {
            await using var cmd = new NpgsqlCommand(
                "SELECT is_identity FROM information_schema.columns " +
                "WHERE table_schema='public' AND table_name=@t AND column_name='id'", conn);
            cmd.Parameters.AddWithValue("t", table);
            return (string?)await cmd.ExecuteScalarAsync();
        }

        Assert.Equal("YES", await IsIdentityAsync("users"));

        await using var typeCmd = new NpgsqlCommand(
            "SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='figures' AND column_name='id'",
            conn);
        Assert.Equal("uuid", (string?)await typeCmd.ExecuteScalarAsync());

        await using var defaultCmd = new NpgsqlCommand(
            "SELECT column_default FROM information_schema.columns WHERE table_schema='public' AND table_name='figures' AND column_name='id'",
            conn);
        Assert.Contains("gen_random_uuid()", (string?)await defaultCmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task FiguresAnchorGeometryColumns_HaveExpectedTypes()
    {
        await using var conn = await OpenConnectionAsync();
        var types = await QueryStringsAsync(
            conn,
            """
            SELECT column_name || ':' || data_type
            FROM information_schema.columns
            WHERE table_schema='public'
              AND table_name='figures'
              AND column_name IN ('x','y','geometry','z')
            ORDER BY column_name
            """);

        Assert.Equal(
            new[] { "geometry:jsonb", "x:integer", "y:integer", "z:numeric" },
            types);
    }

    [Fact]
    public async Task Figures_HasOnlyTypeWhitelistCheckConstraint()
    {
        await using var conn = await OpenConnectionAsync();
        var names = await QueryStringsAsync(
            conn,
            "SELECT conname FROM pg_constraint WHERE conrelid = 'figures'::regclass " +
            "AND contype = 'c' ORDER BY conname");

        Assert.Equal(new[] { "figures_type_is_known" }, names);
    }

    [Fact]
    public async Task IndexOnFiguresUserIdZ_Exists()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT indexname FROM pg_indexes WHERE tablename = 'figures' AND indexname = 'ix_figures_user_id_z'",
            conn);
        var indexName = (string?)await cmd.ExecuteScalarAsync();
        Assert.Equal("ix_figures_user_id_z", indexName);
    }

    [Fact]
    public async Task FiguresTable_HasCommentDocumentingAnchorGeometry()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT obj_description('figures'::regclass)", conn);
        var comment = (string?)await cmd.ExecuteScalarAsync();

        Assert.NotNull(comment);
        Assert.Contains("anchor x,y plus geometry jsonb", comment);
        Assert.DoesNotContain("inscribed in", comment);
    }

    [Fact]
    public async Task Figures_HasNoGeometryCheckConstraint()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT count(*) FROM pg_constraint WHERE conrelid = 'figures'::regclass AND contype = 'c' AND pg_get_constraintdef(oid) ILIKE '%geometry%'",
            conn);

        Assert.Equal(0L, (long)(await cmd.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task UsersUsername_HasAUniqueConstraint()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT i.relname
            FROM pg_index ix
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            WHERE t.relname = 'users' AND a.attname = 'username' AND ix.indisunique
            """,
            conn);
        var indexName = (string?)await cmd.ExecuteScalarAsync();

        Assert.NotNull(indexName);
    }

    [Fact]
    public async Task FiguresUserId_ForeignKey_HasCascadeDeleteRule()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT rc.delete_rule
            FROM information_schema.referential_constraints rc
            JOIN information_schema.table_constraints tc
              ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema
            WHERE tc.table_name = 'figures'
            """,
            conn);
        var deleteRule = (string?)await cmd.ExecuteScalarAsync();

        Assert.Equal("CASCADE", deleteRule);
    }
}
