using BlazorCanvas.Components.Canvas;
using BlazorCanvas.Components.Pages;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tools;
using Bunit;

namespace BlazorCanvas.Tests.Components;

public class PreviewRenderSmokeTests
{
    [Fact]
    public void ActiveStarPreview_RendersPolygonThroughFigureShapeBeforeCommit()
    {
        // TEST-04 / G-15-1 guard: Home is not rendered here because Home.OnInitializedAsync
        // awaits the auth cascade and calls CanvasRepository.EnsureForOwnerAsync and
        // FigureRepository.LoadAsync against PostgreSQL. FigureShape is the component whose
        // Registry.Contains(PreviewType) preview gate blanked the Phase 15 preview.
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var session = BeginActiveStarPreview();

        Assert.True(session.IsActive);

        var component = context.Render<FigureShape>(parameters => parameters
            .Add(component => component.PreviewPlacement, session.Placement)
            .Add(component => component.PreviewType, session.Type)
            .Add(component => component.Selectable, false));

        var polygon = Assert.Single(component.FindAll("polygon"));
        var points = polygon.GetAttribute("points");
        Assert.False(string.IsNullOrWhiteSpace(points));
    }

    [Fact]
    public void RegistryUnknownPreviewType_EmitsNoPolygonForG15LiteralBindingRegression()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var session = BeginActiveStarPreview();

        var component = context.Render<FigureShape>(parameters => parameters
            .Add(component => component.PreviewPlacement, session.Placement)
            .Add(component => component.PreviewType, "preview.Type")
            .Add(component => component.Selectable, false));

        Assert.Empty(component.FindAll("polygon"));
    }

    private static DrawingPreviewSession BeginActiveStarPreview()
    {
        var registry = DefaultShapes.CreateRegistry();
        var session = new DrawingPreviewSession(registry);
        var starType = ToolMap.ToShapeName(Tool.Star);

        Assert.Equal("star5", starType);
        session.Begin(starType!, new CanvasPoint(40, 50));
        session.Update(new CanvasPoint(220, 180));

        return session;
    }
}
