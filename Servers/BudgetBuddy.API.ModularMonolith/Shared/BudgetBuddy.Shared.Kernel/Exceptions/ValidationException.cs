namespace BudgetBuddy.Shared.Kernel.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message) { }
}
