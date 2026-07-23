namespace BlazorCanvas.Shapes;

/// <summary>
/// Holds shape definitions by their exact, case-sensitive database names.
/// </summary>
public sealed class ShapeRegistry
{
    private readonly Dictionary<string, IShapeDefinition> _byName = new(StringComparer.Ordinal);
    private readonly List<IShapeDefinition> _definitions = [];
    private readonly List<string> _names = [];

    /// <summary>
    /// Gets definitions in their registration order.
    /// </summary>
    public IReadOnlyList<IShapeDefinition> All => _definitions;

    /// <summary>
    /// Gets definition names in their registration order.
    /// </summary>
    public IReadOnlyList<string> Names => _names;

    /// <summary>
    /// Registers one definition without allowing an existing definition to be replaced.
    /// </summary>
    public void Register(IShapeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new ArgumentException("Shape definition names cannot be null, empty, or whitespace.", nameof(definition));
        }

        if (!_byName.TryAdd(definition.Name, definition))
        {
            throw new ArgumentException($"A shape definition named '{definition.Name}' is already registered.", nameof(definition));
        }

        _definitions.Add(definition);
        _names.Add(definition.Name);
    }

    /// <summary>
    /// Gets whether an exact registered name has a definition.
    /// </summary>
    public bool Contains(string? name) => TryGet(name, out _);

    /// <summary>
    /// Attempts to resolve an exact registered name and never supplies a fallback definition.
    /// </summary>
    public bool TryGet(string? name, out IShapeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(name) || !_byName.TryGetValue(name, out definition!))
        {
            definition = null!;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves an exact registered name.
    /// </summary>
    public IShapeDefinition Get(string? name)
    {
        if (TryGet(name, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"No shape definition is registered for '{name}'.");
    }
}
