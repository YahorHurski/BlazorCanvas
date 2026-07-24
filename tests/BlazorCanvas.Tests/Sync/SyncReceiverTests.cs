using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using BlazorCanvas.Sync;

namespace BlazorCanvas.Tests.Sync;

/// <summary>
/// Proves the D-40 / D-53 receiver rules SyncReceiver owns: draw is the only kind that may create
/// a figure, move and rollback are UPDATE-ONLY (an unknown id is ignored, never resurrected), delete
/// is idempotent, and applying any message twice is identical to applying it once. Pure unit tests —
/// no database, no fixture, no <c>Database</c> collection — proving the whole surface is testable
/// without a component-test harness (D-49, PA-6).
/// </summary>
public class SyncReceiverTests
{
    private const int UserId = 42;
    private static readonly Guid KnownId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UnknownId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Sender = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static TheoryData<FigureType> AllFigureTypes => new()
    {
        FigureType.Line, FigureType.Rectangle, FigureType.Circle, FigureType.Triangle,
    };

    private static Figure MakeFigure(Guid id, FigureType type, Box box, decimal z) =>
        MakeFigure(id, type, box, z, UserId);

    private static Figure MakeFigure(Guid id, FigureType type, Box box, decimal z, int userId)
    {
        var encoded = GeometryCodec.Encode(type, box);
        return new Figure
        {
            Id = id,
            UserId = userId,
            Type = FigureTypeNames.ToDbValue(type),
            X = encoded.X,
            Y = encoded.Y,
            Geometry = encoded.Geometry,
            Z = z,
        };
    }

    [Fact]
    public void Draw_ForUnknownId_InsertsWithAppendedZ()
    {
        var existing = MakeFigure(OtherId, FigureType.Rectangle, new Box(0, 0, 10, 10), 3m);
        var figures = new List<Figure> { existing };
        var encoded = GeometryCodec.Encode(FigureType.Circle, new Box(20, 20, 40, 40));
        var message = new SyncMessage("draw", Sender, KnownId, "circle", encoded.X, encoded.Y, encoded.Geometry);

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Null(removed);
        Assert.Equal(2, figures.Count);
        var inserted = Assert.Single(figures, f => f.Id == KnownId);
        Assert.Equal("circle", inserted.Type);
        Assert.Equal(encoded.X, inserted.X);
        Assert.Equal(encoded.Y, inserted.Y);
        Assert.Equal(encoded.Geometry, inserted.Geometry);
        Assert.Equal(4m, inserted.Z);
        Assert.Same(inserted, figures[^1]);
    }

    [Fact]
    public void Draw_ForKnownId_DoesNotDuplicate()
    {
        var existing = MakeFigure(KnownId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m);
        var figures = new List<Figure> { existing };
        var message = new SyncMessage("draw", Sender, KnownId, "rectangle", 0, 0, """{"w":10,"h":10}""");

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Null(removed);
        Assert.Single(figures);
        Assert.Same(existing, figures[0]);
    }

    [Fact]
    public void Move_ForUnknownId_LeavesListUnchanged_AndCreatesNothing()
    {
        var existing = MakeFigure(OtherId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m);
        var figures = new List<Figure> { existing };
        var message = SyncMessage.Move(UnknownId, 99, 99, Sender);

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Null(removed);
        Assert.Single(figures);
        Assert.Same(existing, figures[0]);
    }

    [Fact]
    public void Rollback_ForUnknownId_LeavesListUnchanged_AndCreatesNothing()
    {
        var existing = MakeFigure(OtherId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m);
        var figures = new List<Figure> { existing };
        var message = SyncMessage.Rollback(UnknownId, 99, 99, Sender);

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Null(removed);
        Assert.Single(figures);
        Assert.Same(existing, figures[0]);
    }

    [Theory]
    [MemberData(nameof(AllFigureTypes))]
    public void Move_OnKnownFigure_ChangesAnchor_LeavesGeometryByteIdentical(FigureType type)
    {
        var figure = MakeFigure(KnownId, type, new Box(0, 0, 10, 10), 1m);
        var originalGeometry = figure.Geometry;
        var figures = new List<Figure> { figure };
        var message = SyncMessage.Move(KnownId, 500, 600, Sender);

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Null(removed);
        Assert.Equal(500, figure.X);
        Assert.Equal(600, figure.Y);
        Assert.Equal(originalGeometry, figure.Geometry);
    }

    [Fact]
    public void Delete_RemovesAndReturnsTheId()
    {
        var figure = MakeFigure(KnownId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m);
        var figures = new List<Figure> { figure };
        var message = SyncMessage.Delete(KnownId, Sender);

        var removed = SyncReceiver.Apply(figures, message, UserId);

        Assert.Equal(KnownId, removed);
        Assert.Empty(figures);
    }

    [Fact]
    public void Delete_OfUnknownId_ReturnsNull_AndThrowsNothing()
    {
        var figures = new List<Figure> { MakeFigure(OtherId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m) };
        var message = SyncMessage.Delete(UnknownId, Sender);

        Guid? removed = null;
        var exception = Record.Exception(() => removed = SyncReceiver.Apply(figures, message, UserId));

        Assert.Null(exception);
        Assert.Null(removed);
        Assert.Single(figures);
    }

    public static TheoryData<string, SyncMessage> IdempotentMessages
    {
        get
        {
            var drawEncoded = GeometryCodec.Encode(FigureType.Rectangle, new Box(1, 1, 11, 11));
            return new TheoryData<string, SyncMessage>
            {
                { "draw", new SyncMessage("draw", Sender, KnownId, "rectangle", drawEncoded.X, drawEncoded.Y, drawEncoded.Geometry) },
                { "move", SyncMessage.Move(KnownId, 250, 260, Sender) },
                { "rollback", SyncMessage.Rollback(KnownId, 10, 20, Sender) },
                { "delete", SyncMessage.Delete(KnownId, Sender) },
            };
        }
    }

    /// <summary>
    /// draw's seed omits KnownId so the message genuinely inserts on the first application (proving
    /// the second application is a real no-duplicate, not a no-op-of-a-no-op); move/rollback/delete
    /// need KnownId already present to exercise their update/removal paths.
    /// </summary>
    [Theory]
    [MemberData(nameof(IdempotentMessages))]
    public void Apply_IsIdempotent_ForEveryKind(string kind, SyncMessage message)
    {
        List<Figure> Seed() => kind == "draw"
            ? new List<Figure> { MakeFigure(OtherId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m) }
            : new List<Figure> { MakeFigure(KnownId, FigureType.Rectangle, new Box(0, 0, 10, 10), 1m) };

        var once = Seed();
        var twice = Seed();

        SyncReceiver.Apply(once, message, UserId);

        SyncReceiver.Apply(twice, message, UserId);
        SyncReceiver.Apply(twice, message, UserId);

        Assert.Equal(once.Count, twice.Count);
        for (var i = 0; i < once.Count; i++)
        {
            AssertFigureEqual(once[i], twice[i]);
        }
    }

    private static Figure Clone(Figure figure) => new()
    {
        Id = figure.Id,
        UserId = figure.UserId,
        Type = figure.Type,
        X = figure.X,
        Y = figure.Y,
        Geometry = figure.Geometry,
        Z = figure.Z,
    };

    private static void AssertFigureEqual(Figure expected, Figure actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.Geometry, actual.Geometry);
        Assert.Equal(expected.Z, actual.Z);
    }
}
