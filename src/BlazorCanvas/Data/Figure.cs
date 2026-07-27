namespace BlazorCanvas.Data;

/// <summary>
/// A drawn figure stored as a D-59 anchor plus geometry JSON. `Type` is a plain `string`,
/// deliberately (D-46): the database CHECK constraint is written against lowercase literals, and a
/// C# enum mapping would add another conversion that must stay in lockstep with that whitelist.
/// </summary>
public class Figure
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public string Geometry { get; set; } = string.Empty;

    public decimal Z { get; set; }
}
