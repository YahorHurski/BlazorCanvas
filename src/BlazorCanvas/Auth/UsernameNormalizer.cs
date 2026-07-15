namespace BlazorCanvas.Auth;

/// <summary>
/// The single source of the username case-insensitivity rule (D-44). Postgres's default collation
/// is case-sensitive, so the `users.username` UNIQUE index does nothing to fold "Egor" and "egor"
/// together on its own - that is 100% an application responsibility. Applied once, before every
/// lookup and every INSERT, in exactly one place - never duplicated inline.
/// </summary>
public static class UsernameNormalizer
{
    public static string Normalize(string? username) => (username ?? "").Trim().ToLowerInvariant();
}
