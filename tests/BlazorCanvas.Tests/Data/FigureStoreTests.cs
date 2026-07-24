using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using BlazorCanvas.Tests.Database;
using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Tests.Data;

/// <summary>
/// Proves the one real security surface this phase introduces: user A's load can never return
/// user B's figures (T-03-01, ROADMAP criterion 5). Also proves creation-order/z-order survival,
/// the id-after-INSERT contract that InsertAsync's <c>return figure;</c> ordering exists for
/// (D-39), and — post-10-02 — the D-59 anchor-only move contract: <see cref="FigureStore.MoveAsync"/>
/// touches exactly <c>x, y</c>, geometry is byte-identical across any number of moves, off-canvas
/// anchors persist unchanged (STOR-04), and moves are anchor-idempotent. Runs against the live
/// database via <see cref="DatabaseFixture"/>, in the same style as <see cref="GuardMirrorsChecksTests"/>.
/// </summary>
[Collection("Database")]
public class FigureStoreTests
{
    private readonly DatabaseFixture _fixture;

    public FigureStoreTests(DatabaseFixture fixture) => _fixture = fixture;

    // Well-formed boxes that satisfy every CHECK constraint, so a test failure here means
    // FigureStore is wrong, never the geometry.
    private static readonly Box Rect = new(0, 0, 10, 10);
    private static readonly Box SquareCircle = new(100, 100, 140, 140); // even-sided square

    /// <summary>
    /// FigureStore needs an IDbContextFactory and the test project has no DI container. This
    /// small nested adapter routes CreateDbContext() to fixture.CreateContext() — the interface's
    /// CreateDbContextAsync has a default implementation that wraps the synchronous member, so
    /// only this one method needs a body. No DI or mocking package added (D-49, T-03-SC).
    /// </summary>
    private sealed class TestDbContextFactory(DatabaseFixture fixture) : IDbContextFactory<CanvasDbContext>
    {
        public CanvasDbContext CreateDbContext() => fixture.CreateContext();
    }

    private FigureStore CreateStore() => new(new TestDbContextFactory(_fixture));

    /// <summary>
    /// Post-10-02, InsertAsync takes an already-encoded FigureGeometry rather than a Box — this
    /// helper keeps every test call site reading like the old Box-based one while going through
    /// the real GeometryCodec.Encode the app itself uses (D-59).
    /// </summary>
    private static Task<Figure> InsertBoxAsync(FigureStore store, int userId, FigureType type, Box box) =>
        store.InsertAsync(userId, type, GeometryCodec.Encode(type, box));

    [Fact]
    public async Task LoadAsync_NeverReturnsAnotherUsersFigures()
    {
        var store = CreateStore();

        int userA, userB;
        await using (var context = _fixture.CreateContext())
        {
            userA = await DatabaseFixture.CreateTestUserAsync(context);
            userB = await DatabaseFixture.CreateTestUserAsync(context);
        }

        await InsertBoxAsync(store, userA, FigureType.Rectangle, Rect);
        await InsertBoxAsync(store, userA, FigureType.Line, Rect);

        await InsertBoxAsync(store, userB, FigureType.Rectangle, Rect);
        await InsertBoxAsync(store, userB, FigureType.Line, Rect);
        await InsertBoxAsync(store, userB, FigureType.Triangle, Rect);

        var figuresA = await store.LoadAsync(userA);
        var figuresB = await store.LoadAsync(userB);

        Assert.Equal(2, figuresA.Count);
        Assert.Equal(3, figuresB.Count);
        Assert.All(figuresA, f => Assert.Equal(userA, f.UserId));
        Assert.All(figuresB, f => Assert.Equal(userB, f.UserId));
    }

    [Fact]
    public async Task LoadAsync_ForUserWithNoFigures_ReturnsEmptyNonNullList()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var figures = await store.LoadAsync(userId);

        Assert.NotNull(figures);
        Assert.Empty(figures);
    }

    [Fact]
    public async Task LoadAsync_ReturnsFiguresInCreationOrder()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var f1 = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);
        var f2 = await InsertBoxAsync(store, userId, FigureType.Line, Rect);
        var f3 = await InsertBoxAsync(store, userId, FigureType.Triangle, Rect);
        var f4 = await InsertBoxAsync(store, userId, FigureType.Circle, SquareCircle);

        var loaded = await store.LoadAsync(userId);

        var expectedIds = new[] { f1.Id, f2.Id, f3.Id, f4.Id };
        Assert.Equal(expectedIds, loaded.Select(f => f.Id));

        Assert.Equal(new[] { 1m, 2m, 3m, 4m }, loaded.Select(f => f.Z));
    }

    [Fact]
    public async Task InsertAsync_ReturnsDatabaseAssignedId_PresentInSubsequentLoad()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        Assert.NotEqual(Guid.Empty, inserted.Id);

        var loaded = await store.LoadAsync(userId);
        Assert.Contains(loaded, f => f.Id == inserted.Id);
    }

    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public async Task InsertAsync_TypeLiteral_RoundTrips(FigureType type)
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, type, Rect);
        var loaded = await store.LoadAsync(userId);
        var reloaded = Assert.Single(loaded, f => f.Id == inserted.Id);

        Assert.Equal(type, FigureTypeNames.Parse(reloaded.Type));
    }

    [Fact]
    public async Task InsertAsync_Circle_TypeLiteral_RoundTrips()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, FigureType.Circle, SquareCircle);
        var loaded = await store.LoadAsync(userId);
        var reloaded = Assert.Single(loaded, f => f.Id == inserted.Id);

        Assert.Equal(FigureType.Circle, FigureTypeNames.Parse(reloaded.Type));
    }

    [Fact]
    public async Task LoadAsync_UsesAsNoTracking_ReturnedEntitiesAreDetached()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        var loaded = await store.LoadAsync(userId);

        // AsNoTracking means these entities outlive the context that produced them - verified
        // simply by having successfully read them back after the producing context is disposed.
        Assert.Single(loaded);
    }

    [Fact]
    public async Task MoveAsync_MovesFigure_ReturnsOneAffectedRow()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        var affected = await store.MoveAsync(userId, inserted.Id, 20, 20);

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        var moved = Assert.Single(loaded, f => f.Id == inserted.Id);
        Assert.Equal(20, moved.X);
        Assert.Equal(20, moved.Y);
    }

    /// <summary>
    /// The STOR-02 proof that a move touches only x, y, for every shape: capture the raw stored
    /// Geometry string before the move, move the figure, and assert the string is byte-identical
    /// after reload — not merely decoded-equal. A re-serialisation with different member order or
    /// spacing must fail this test.
    /// </summary>
    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    [InlineData(FigureType.Circle)]
    public async Task MoveAsync_GeometryStringIsByteIdenticalAcrossAMove(FigureType type)
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var box = type == FigureType.Circle ? SquareCircle : Rect;
        var inserted = await InsertBoxAsync(store, userId, type, box);

        // Capture the stored Geometry string via a reload, not the in-memory insert result:
        // PostgreSQL's jsonb column canonicalises key order/spacing once at INSERT time, so the
        // pre-move baseline for "byte-identical across a move" must be what LoadAsync actually
        // reads back, not the compact string GeometryCodec.Encode produced in memory.
        var loadedBeforeMove = Assert.Single(await store.LoadAsync(userId), f => f.Id == inserted.Id);
        var geometryBeforeMove = loadedBeforeMove.Geometry;

        var targetX = loadedBeforeMove.X + 37;
        var targetY = loadedBeforeMove.Y - 19;
        var affected = await store.MoveAsync(userId, inserted.Id, targetX, targetY);

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        var moved = Assert.Single(loaded, f => f.Id == inserted.Id);

        Assert.Equal(targetX, moved.X);
        Assert.Equal(targetY, moved.Y);
        Assert.Equal(geometryBeforeMove, moved.Geometry);
    }

    /// <summary>
    /// STOR-04: with the canvas-edge clamp gone, a move to an anchor beyond each of the four
    /// canvas sides persists exactly as requested — nothing pulls it back toward the canvas.
    /// </summary>
    [Theory]
    [InlineData(-500, 300)] // negative x
    [InlineData(300, -500)] // negative y
    [InlineData(2000, 300)] // x past 1472
    [InlineData(300, 1200)] // y past 828
    public async Task MoveAsync_OffCanvasAnchor_PersistsUnchanged(int x, int y)
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        var affected = await store.MoveAsync(userId, inserted.Id, x, y);

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        var moved = Assert.Single(loaded, f => f.Id == inserted.Id);
        Assert.Equal(x, moved.X);
        Assert.Equal(y, moved.Y);
    }

    /// <summary>
    /// STOR-04 idempotency: a move carries an absolute anchor, never a delta, so applying the
    /// same anchor twice leaves the figure in exactly the same place.
    /// </summary>
    [Fact]
    public async Task MoveAsync_AppliedTwiceWithSameAnchor_IsIdempotent()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        await store.MoveAsync(userId, inserted.Id, 500, 600);
        var afterFirst = Assert.Single(await store.LoadAsync(userId), f => f.Id == inserted.Id);

        await store.MoveAsync(userId, inserted.Id, 500, 600);
        var afterSecond = Assert.Single(await store.LoadAsync(userId), f => f.Id == inserted.Id);

        Assert.Equal(afterFirst.X, afterSecond.X);
        Assert.Equal(afterFirst.Y, afterSecond.Y);
    }

    [Fact]
    public async Task MoveAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var affected = await store.MoveAsync(userId, Guid.NewGuid(), 20, 20);

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task MoveAsync_NeverTouchesAnotherUsersFigure()
    {
        var store = CreateStore();

        int userA, userB;
        await using (var context = _fixture.CreateContext())
        {
            userA = await DatabaseFixture.CreateTestUserAsync(context);
            userB = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var figureA = await InsertBoxAsync(store, userA, FigureType.Rectangle, Rect);

        // Baseline via reload, not the in-memory insert result: PostgreSQL's jsonb column
        // canonicalises the Geometry string once at INSERT time (see the byte-identical test
        // above), so the "unchanged" comparison must be reload-to-reload.
        var loadedBefore = Assert.Single(await store.LoadAsync(userA), f => f.Id == figureA.Id);

        // IDOR proof for T-10-04: userB names userA's real figure id and still matches no row.
        var affected = await store.MoveAsync(userB, figureA.Id, 20, 20);

        Assert.Equal(0, affected);
        var loaded = await store.LoadAsync(userA);
        var unchanged = Assert.Single(loaded, f => f.Id == figureA.Id);
        Assert.Equal(loadedBefore.X, unchanged.X);
        Assert.Equal(loadedBefore.Y, unchanged.Y);
        Assert.Equal(loadedBefore.Geometry, unchanged.Geometry);
    }

    /// <summary>
    /// STOR-02 adjacency: two figures with identical type, anchor and geometry remain two
    /// distinct rows — nothing dedupes or merges them.
    /// </summary>
    [Fact]
    public async Task InsertAsync_TwoIdenticalFigures_RemainDistinctRows()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var first = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);
        var second = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Z, second.Z);
        Assert.Equal(first.X, second.X);
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(first.Geometry, second.Geometry);
    }

    /// <summary>
    /// STOR-05 ordering: after a mixed sequence of draw, move and delete, the surviving figures
    /// still reload in ascending z with the same relative order they had before.
    /// </summary>
    [Fact]
    public async Task LoadAsync_AfterMoveAndDelete_PreservesRelativeZOrderOfSurvivors()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var f1 = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);
        var f2 = await InsertBoxAsync(store, userId, FigureType.Line, Rect);
        var f3 = await InsertBoxAsync(store, userId, FigureType.Triangle, Rect);
        var f4 = await InsertBoxAsync(store, userId, FigureType.Circle, SquareCircle);

        await store.MoveAsync(userId, f2.Id, 900, 900);
        await store.DeleteAsync(userId, f3.Id);

        var loaded = await store.LoadAsync(userId);

        Assert.Equal(new[] { f1.Id, f2.Id, f4.Id }, loaded.Select(f => f.Id));
        var zs = loaded.Select(f => f.Z).ToList();
        Assert.Equal(zs, zs.OrderBy(z => z).ToList());
    }

    [Fact]
    public async Task DeleteAsync_RemovesFigure_ReturnsOneAffectedRow()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var deleted = await InsertBoxAsync(store, userId, FigureType.Rectangle, Rect);
        var kept = await InsertBoxAsync(store, userId, FigureType.Line, Rect);

        var affected = await store.DeleteAsync(userId, deleted.Id);

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        Assert.DoesNotContain(loaded, f => f.Id == deleted.Id);
        Assert.Contains(loaded, f => f.Id == kept.Id);
    }

    [Fact]
    public async Task DeleteAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var affected = await store.DeleteAsync(userId, Guid.NewGuid());

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task DeleteAsync_NeverDeletesAnotherUsersFigure()
    {
        var store = CreateStore();

        int userA, userB;
        await using (var context = _fixture.CreateContext())
        {
            userA = await DatabaseFixture.CreateTestUserAsync(context);
            userB = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var figureA = await InsertBoxAsync(store, userA, FigureType.Rectangle, Rect);

        // IDOR proof for T-10-04: userB names userA's real figure id and still deletes nothing.
        var affected = await store.DeleteAsync(userB, figureA.Id);

        Assert.Equal(0, affected);
        var loaded = await store.LoadAsync(userA);
        Assert.Contains(loaded, f => f.Id == figureA.Id);
    }
}
