namespace BudgetBuddy.Shared.Kernel.Exceptions;

/// <summary>
/// Base exception for domain/business rule violations
/// </summary>
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}