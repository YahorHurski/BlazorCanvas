namespace BlazorCanvas.Shapes;

/// <summary>
/// Builds the canonical registry used for persistence seeding and shape dispatch.
/// </summary>
public static class DefaultShapes
{
    /// <summary>
    /// Creates a new registry in v1.1 enum and <c>figure_types</c> seed order.
    /// A fresh instance prevents test-only registrations from leaking into production seeding.
    /// </summary>
    public static ShapeRegistry CreateRegistry()
    {
        var registry = new ShapeRegistry();
        registry.Register(new LineShape());
        registry.Register(new RectangleShape());
        registry.Register(new CircleShape());
        registry.Register(new TriangleShape());
        registry.Register(new Star5Shape());
        return registry;
    }
}
