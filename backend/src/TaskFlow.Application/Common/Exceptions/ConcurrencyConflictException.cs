namespace TaskFlow.Application.Common.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message = "Someone else changed this card. Refresh and try again.")
        : base(message)
    {
    }
}
