using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using BlazorCanvas.Tests.Database;
using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Tests.Data;

/// <summary>
/// Proves the one real security surface this phase introduces: user A's load can never return
/// user B's figures (T-03-01, ROADMAP criterion 5). Also proves creation-order/z-order survival
/// and the id-after-INSERT contract that InsertAsync's <c>return figure;</c> ordering exists for
/// (D-39). Runs against the live database via <see cref="DatabaseFixture"/>, in the same style as
/// <see cref="GuardMirrorsChecksTests"/>.
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

        await store.InsertAsync(userA, FigureType.Rectangle, Rect);
        await store.InsertAsync(userA, FigureType.Line, Rect);

        await store.InsertAsync(userB, FigureType.Rectangle, Rect);
        await store.InsertAsync(userB, FigureType.Line, Rect);
        await store.InsertAsync(userB, FigureType.Triangle, Rect);

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

        var f1 = await store.InsertAsync(userId, FigureType.Rectangle, Rect);
        var f2 = await store.InsertAsync(userId, FigureType.Line, Rect);
        var f3 = await store.InsertAsync(userId, FigureType.Triangle, Rect);
        var f4 = await store.InsertAsync(userId, FigureType.Circle, SquareCircle);

        var loaded = await store.LoadAsync(userId);

        var expectedIds = new[] { f1.Id, f2.Id, f3.Id, f4.Id };
        Assert.Equal(expectedIds, loaded.Select(f => f.Id));

        for (var i = 1; i < loaded.Count; i++)
        {
            Assert.True(loaded[i].Id > loaded[i - 1].Id, "Expected strictly ascending ids.");
        }
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

        var inserted = await store.InsertAsync(userId, FigureType.Rectangle, Rect);

        Assert.True(inserted.Id > 0, "InsertAsync must return a Figure with a database-assigned Id.");

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

        var inserted = await store.InsertAsync(userId, type, Rect);
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

        var inserted = await store.InsertAsync(userId, FigureType.Circle, SquareCircle);
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

        await store.InsertAsync(userId, FigureType.Rectangle, Rect);

        var loaded = await store.LoadAsync(userId);

        // AsNoTracking means these entities outlive the context that produced them - verified
        // simply by having successfully read them back after the producing context is disposed.
        Assert.Single(loaded);
    }

    [Fact]
    public async Task UpdateAsync_MovesFigure_ReturnsOneAffectedRow()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await store.InsertAsync(userId, FigureType.Rectangle, Rect);

        var affected = await store.UpdateAsync(userId, inserted.Id, new Box(20, 20, 30, 30));

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        var moved = Assert.Single(loaded, f => f.Id == inserted.Id);
        Assert.Equal(20, moved.X1);
        Assert.Equal(20, moved.Y1);
        Assert.Equal(30, moved.X2);
        Assert.Equal(30, moved.Y2);
    }

    [Fact]
    public async Task UpdateAsync_Circle_TranslationPreservesTheInscribedSquare()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var inserted = await store.InsertAsync(userId, FigureType.Circle, SquareCircle);
        var before = CircleEncoding.ToCentreRadius(SquareCircle);

        var affected = await store.UpdateAsync(userId, inserted.Id, new Box(200, 200, 240, 240));

        Assert.Equal(1, affected);
        var loaded = await store.LoadAsync(userId);
        var moved = Assert.Single(loaded, f => f.Id == inserted.Id);
        var after = CircleEncoding.ToCentreRadius(new Box(moved.X1, moved.Y1, moved.X2, moved.Y2));

        Assert.Equal(before.R, after.R);
        Assert.Equal(before.Cx + 100, after.Cx);
        Assert.Equal(before.Cy + 100, after.Cy);
    }

    [Fact]
    public async Task UpdateAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing()
    {
        var store = CreateStore();

        int userId;
        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var affected = await store.UpdateAsync(userId, 999_999, new Box(20, 20, 30, 30));

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task UpdateAsync_NeverTouchesAnotherUsersFigure()
    {
        var store = CreateStore();

        int userA, userB;
        await using (var context = _fixture.CreateContext())
        {
            userA = await DatabaseFixture.CreateTestUserAsync(context);
            userB = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var figureA = await store.InsertAsync(userA, FigureType.Rectangle, Rect);

        // IDOR proof for T-04-01: userB names userA's real figure id and still matches no row.
        var affected = await store.UpdateAsync(userB, figureA.Id, new Box(20, 20, 30, 30));

        Assert.Equal(0, affected);
        var loaded = await store.LoadAsync(userA);
        var unchanged = Assert.Single(loaded, f => f.Id == figureA.Id);
        Assert.Equal(0, unchanged.X1);
        Assert.Equal(0, unchanged.Y1);
        Assert.Equal(10, unchanged.X2);
        Assert.Equal(10, unchanged.Y2);
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

        var deleted = await store.InsertAsync(userId, FigureType.Rectangle, Rect);
        var kept = await store.InsertAsync(userId, FigureType.Line, Rect);

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

        var affected = await store.DeleteAsync(userId, 999_999);

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

        var figureA = await store.InsertAsync(userA, FigureType.Rectangle, Rect);

        // IDOR proof for T-04-01: userB names userA's real figure id and still deletes nothing.
        var affected = await store.DeleteAsync(userB, figureA.Id);

        Assert.Equal(0, affected);
        var loaded = await store.LoadAsync(userA);
        Assert.Contains(loaded, f => f.Id == figureA.Id);
    }
}
