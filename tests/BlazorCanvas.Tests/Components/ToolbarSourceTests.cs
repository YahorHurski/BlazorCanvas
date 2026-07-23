namespace BlazorCanvas.Tests.Components;

public class ToolbarSourceTests
{
    [Fact]
    public void ToolbarSource_OrdersStarBetweenTriangleAndDelete()
    {
        var toolbar = Source("Toolbar.razor");

        var pointer = toolbar.IndexOf("ToolButtonClass(Tool.Pointer)", StringComparison.Ordinal);
        var line = toolbar.IndexOf("ToolButtonClass(Tool.Line)", StringComparison.Ordinal);
        var rectangle = toolbar.IndexOf("ToolButtonClass(Tool.Rectangle)", StringComparison.Ordinal);
        var circle = toolbar.IndexOf("ToolButtonClass(Tool.Circle)", StringComparison.Ordinal);
        var triangle = toolbar.IndexOf("ToolButtonClass(Tool.Triangle)", StringComparison.Ordinal);
        var star = toolbar.IndexOf("ToolButtonClass(Tool.Star)", StringComparison.Ordinal);
        var delete = toolbar.IndexOf("delete-button", StringComparison.Ordinal);

        Assert.True(pointer < line);
        Assert.True(line < rectangle);
        Assert.True(rectangle < circle);
        Assert.True(circle < triangle);
        Assert.True(triangle < star);
        Assert.True(star < delete);
    }

    [Fact]
    public void ToolbarSource_StarUsesArmableButtonPattern()
    {
        var toolbar = Source("Toolbar.razor");
        var starButtonStart = toolbar.IndexOf("ToolButtonClass(Tool.Star)", StringComparison.Ordinal);
        var deleteButtonStart = toolbar.IndexOf("delete-button", StringComparison.Ordinal);

        Assert.True(starButtonStart >= 0);
        Assert.True(deleteButtonStart > starButtonStart);
        var starButton = toolbar[starButtonStart..deleteButtonStart];

        Assert.Contains("aria-pressed=\"@(Armed == Tool.Star)\"", starButton);
        Assert.Contains("aria-label=\"Draw star\"", starButton);
        Assert.Contains("ArmedChanged.InvokeAsync(Tool.Star)", starButton);
        Assert.Contains("width=\"20\" height=\"20\" viewBox=\"0 0 20 20\"", starButton);
        Assert.Contains("stroke=\"currentColor\"", starButton);
    }

    [Fact]
    public void ToolbarSource_PreservesLogoutPostFormAndAntiforgeryInput()
    {
        var toolbar = Source("Toolbar.razor");

        Assert.Contains("<form method=\"post\" action=\"/logout\" class=\"logout-form\">", toolbar);
        Assert.Contains("<input type=\"hidden\" name=\"@token!.FormFieldName\" value=\"@token.Value\" />", toolbar);
    }

    [Fact]
    public void ToolbarStyles_PreserveStripHeightAndLogoutAlignment()
    {
        var css = Source("Toolbar.razor.css");

        Assert.Contains("height: 48px;", css);
        Assert.Contains("margin-left: auto;", css);
    }

    private static string Source(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "BlazorCanvas", "Components", "Canvas", name);
            if (File.Exists(path)) return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException("Could not locate the repository source tree.");
    }
}
