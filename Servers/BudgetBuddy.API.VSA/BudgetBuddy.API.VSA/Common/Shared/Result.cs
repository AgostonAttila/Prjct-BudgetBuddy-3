namespace BudgetBuddy.API.VSA.Common.Shared;

/// <summary>
/// Represents the result of an operation (success or failure)
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string errorMessage) => new(false, errorMessage);

    public static Result<T> Ok<T>(T value) => new(value, true, null);
    public static Result<T> Fail<T>(string errorMessage) => new(default, false, errorMessage);
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Value = value;
    }
}
