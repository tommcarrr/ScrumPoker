namespace ScrumPoker.Domain;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) {}
}
