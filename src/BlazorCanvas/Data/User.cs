namespace BlazorCanvas.Data;

/// <summary>
/// A registered user. "Whose canvas do I load?" (D-44). Password is stored and compared in
/// plaintext — locked and deliberate (D-08); this is a throwaway learning project only.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
