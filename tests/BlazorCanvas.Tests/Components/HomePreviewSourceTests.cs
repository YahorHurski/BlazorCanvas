using System.Globalization;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Components;

public class HomePreviewSourceTests
{
    [Fact]
    public void Home_RendersActiveDrawingPreviewThroughFigureShapeAfterPersistedFiguresAndSelectionTrace()
    {
        var source = Source("Home.razor");
        var svgBody = ExtractBetween(source, "<svg", "</svg>");
        var previewBlock = ExtractBetween(
            svgBody,
            "if (preview?.IsActive == true && preview.Placement is not null)",
            "/>");

        Assert.Contains("<FigureShape", previewBlock);
        Assert.Contains("PreviewPlacement=\"preview.Placement\"", previewBlock);
        Assert.Contains("PreviewType=\"@preview.Type\"", previewBlock);
        Assert.DoesNotContain("PreviewType=\"preview.Type\"", previewBlock);
        Assert.Contains("Selectable=\"false\"", previewBlock);
        Assert.Contains("SelectionTrace Figure=\"selected\"", svgBody);
        Assert.True(
            svgBody.LastIndexOf("PreviewPlacement=\"preview.Placement\"", StringComparison.Ordinal)
            > svgBody.LastIndexOf("SelectionTrace Figure=\"selected\"", StringComparison.Ordinal),
            "The local preview must render after persisted figures and the selection trace.");
        Assert.DoesNotContain("Notifier.", previewBlock);
        Assert.DoesNotContain("CanvasSyncNotifier", previewBlock);
    }

    [Fact]
    public void HomeScript_KeepsPointerCaptureCleanupButDoesNotOwnPreviewGeometry()
    {
        var script = Source("Home.razor.js");
        var innerRatioLiteral = Star5Shape.DefaultInnerRatio.ToString("0.###", CultureInfo.InvariantCulture);

        Assert.Contains("setPointerCapture", script);
        Assert.Contains("releasePointerCapture", script);
        Assert.Contains("data-local-drawing-preview", script);
        Assert.Contains("removePreview(surface)", script);

        // TEST-04 / D-70 / D-71: visible star preview geometry must stay in the C# registry path.
        Assert.DoesNotContain("star5", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("innerRatio", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(innerRatioLiteral, script);
        Assert.DoesNotContain($".{innerRatioLiteral.Split('.')[1]}", script);
        Assert.DoesNotContain("document.createElementNS", script);
        Assert.DoesNotContain("svgNamespace", script);
        Assert.DoesNotContain("setAttribute(\"points\"", script);
        Assert.DoesNotContain(".points", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createElementNS(svgNamespace, type === \"line\" ? \"line\" : type === \"rectangle\" ? \"rect\" : type === \"circle\" ? \"circle\" : \"polygon\")", script);
        Assert.DoesNotContain("Math.PI", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Math.cos", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Math.sin", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StarPreviewInnerRatio_IsPinnedToStar5ShapeProductionConstant()
    {
        // TEST-04 / D-70 / D-71: the C# shape definition is the single production source.
        Assert.Equal(0.382, Star5Shape.DefaultInnerRatio);
    }

    private static string Source(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "BlazorCanvas", "Components", "Pages", name);
            if (File.Exists(path)) return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException("Could not locate the repository source tree.");
    }

    private static string ExtractBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find start marker '{startMarker}'.");
        start += startMarker.Length;

        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Could not find end marker '{endMarker}'.");

        return source[start..end];
    }
}
