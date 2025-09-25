namespace ScrumPoker.Domain;

public static class Deck
{
    public static readonly string[] Values = new[] { "0", "1", "2", "3", "5", "8", "13", "20", "40", "100", "?" };

    public static bool IsValid(string value) => Values.Contains(value);
}
