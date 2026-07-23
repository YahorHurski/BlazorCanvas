using BlazorCanvas.Shapes;

namespace BlazorCanvas.Components.Pages;

/// <summary>
/// Holds the ephemeral drawing state for one interactive Blazor circuit. A preview is deliberately
/// not a figure: it has no persistence or synchronization dependencies and exists only until the
/// pointer gesture is handed to the coordinator for validation and commit.
/// </summary>
internal sealed class DrawingPreviewSession
{
    private readonly ShapeRegistry _registry;

    public DrawingPreviewSession(ShapeRegistry registry)
    {
        _registry = registry;
    }

    public string? Type { get; private set; }
    public CanvasPoint? Press { get; private set; }
    public CanvasPoint? Cursor { get; private set; }
    public ShapePlacement? Placement { get; private set; }
    public bool IsActive => Type is not null && Press is not null && Cursor is not null && Placement is not null;

    public void Begin(string type, CanvasPoint press)
    {
        var definition = _registry.Get(type);
        Type = type;
        Press = press;
        Cursor = press;
        Placement = definition.FromGesture(press, press);
    }

    public void Update(CanvasPoint cursor)
    {
        if (!IsActive || Type is null || Press is not { } press)
        {
            return;
        }

        Cursor = cursor;
        Placement = _registry.Get(Type).FromGesture(press, cursor);
    }

    public CompletedDrawGesture? Complete()
    {
        if (!IsActive || Type is null || Press is not { } press || Cursor is not { } cursor)
        {
            return null;
        }

        var completed = new CompletedDrawGesture(Type, press, cursor);
        Clear();
        return completed;
    }

    public void Clear()
    {
        Type = null;
        Press = null;
        Cursor = null;
        Placement = null;
    }
}

/// <summary>
/// Immutable, circuit-local draw input captured before the preview is cleared and passed to the
/// existing coordinator commit boundary.
/// </summary>
internal sealed record CompletedDrawGesture(string Type, CanvasPoint Press, CanvasPoint Cursor);
