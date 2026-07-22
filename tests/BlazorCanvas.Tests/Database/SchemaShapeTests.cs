using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// Asserts the LIVE schema — queried from PostgreSQL's own information_schema and pg_catalog,
/// never from the EF model — is exactly the shape CONSTRAINT-schema specifies (D-12, D-46,
/// D-39, D-42). The EF model is the thing under test here; asserting against it would prove
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
        // This is specifically public: v11.canvases exists until Phase 11 moves it during cutover.
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
            "WHERE table_schema='public' AND table_name='figures' ORDER BY ordinal_position");

        Assert.Equal(new[] { "id", "user_id", "type", "x1", "y1", "x2", "y2" }, columns);
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
    public async Task FiguresId_AndUsersId_AreIdentityColumns()
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

        Assert.Equal("YES", await IsIdentityAsync("figures"));
        Assert.Equal("YES", await IsIdentityAsync("users"));
    }

    [Fact]
    public async Task Figures_HasExactlyFourNamedCheckConstraints()
    {
        await using var conn = await OpenConnectionAsync();
        var names = await QueryStringsAsync(
            conn,
            "SELECT conname FROM pg_constraint WHERE conrelid = 'public.figures'::regclass " +
            "AND contype = 'c' ORDER BY conname");

        Assert.Equal(4, names.Count);
        Assert.Contains("circle_is_a_circle", names);
        Assert.Contains("box_is_a_box", names);
        Assert.Contains("line_is_a_line", names);
        Assert.Contains("figures_type_is_known", names);
    }

    [Fact]
    public async Task IndexOnFiguresUserId_Exists()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'figures' AND indexname = 'ix_figures_user_id'",
            conn);
        var indexName = (string?)await cmd.ExecuteScalarAsync();
        Assert.Equal("ix_figures_user_id", indexName);
    }

    [Fact]
    public async Task FiguresTable_HasCommentDocumentingTheCircleConvention()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT obj_description('public.figures'::regclass)", conn);
        var comment = (string?)await cmd.ExecuteScalarAsync();

        Assert.NotNull(comment);
        Assert.Contains("inscribed in", comment);
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
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            WHERE n.nspname = 'public' AND t.relname = 'users' AND a.attname = 'username' AND ix.indisunique
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
            -- v11.figures has two FKs; without public this scalar query depends on catalog row order.
            WHERE tc.table_name = 'figures' AND tc.table_schema = 'public'
            """,
            conn);
        var deleteRule = (string?)await cmd.ExecuteScalarAsync();

        Assert.Equal("CASCADE", deleteRule);
    }
}
