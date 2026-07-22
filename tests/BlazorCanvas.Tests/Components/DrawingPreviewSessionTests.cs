using BlazorCanvas.Components.Pages;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Components;

public class DrawingPreviewSessionTests
{
    [Fact]
    public void BeginAndUpdate_Rectangle_ExposesTheCurrentLocalPlacement()
    {
        var registry = DefaultShapes.CreateRegistry();
        var session = new DrawingPreviewSession(registry);
        var press = new CanvasPoint(10, 20);
        var cursor = new CanvasPoint(310, 200);

        session.Begin("rectangle", press);

        Assert.True(session.IsActive);
        Assert.Equal("rectangle", session.Type);
        Assert.Equal(press, session.Press);
        Assert.Equal(press, session.Cursor);
        Assert.Equal(registry.Get("rectangle").FromGesture(press, press), session.Placement);

        session.Update(cursor);

        Assert.Equal(cursor, session.Cursor);
        Assert.Equal(registry.Get("rectangle").FromGesture(press, cursor), session.Placement);
    }

    [Theory]
    [InlineData("line", -10, -10, 2000, 1000)]
    [InlineData("circle", 10, 300, 500, 300)]
    [InlineData("triangle", -10, -10, 2000, 1000)]
    public void Update_PreservesRegistryPlacementAndCanvasEdgeClamping(
        string type, double pressX, double pressY, double cursorX, double cursorY)
    {
        var registry = DefaultShapes.CreateRegistry();
        var session = new DrawingPreviewSession(registry);
        var press = new CanvasPoint(pressX, pressY);
        var cursor = new CanvasPoint(cursorX, cursorY);

        session.Begin(type, press);
        session.Update(cursor);

        var expected = registry.Get(type).FromGesture(press, cursor);
        var actual = Assert.IsType<ShapePlacement>(session.Placement);
        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(
            registry.Get(type).ToJson(expected.Geometry),
            registry.Get(type).ToJson(actual.Geometry));
    }

    [Fact]
    public void CompleteAndClear_RemovePlacementAfterCapturingImmutableGesture()
    {
        var session = new DrawingPreviewSession(DefaultShapes.CreateRegistry());
        var press = new CanvasPoint(100, 120);
        var cursor = new CanvasPoint(240, 360);
        session.Begin("triangle", press);
        session.Update(cursor);

        var completed = session.Complete();

        Assert.Equal(new CompletedDrawGesture("triangle", press, cursor), completed);
        Assert.False(session.IsActive);
        Assert.Null(session.Placement);
        Assert.Null(session.Type);
        Assert.Null(session.Press);
        Assert.Null(session.Cursor);

        session.Begin("line", press);
        session.Clear();

        Assert.False(session.IsActive);
        Assert.Null(session.Complete());
    }
}
