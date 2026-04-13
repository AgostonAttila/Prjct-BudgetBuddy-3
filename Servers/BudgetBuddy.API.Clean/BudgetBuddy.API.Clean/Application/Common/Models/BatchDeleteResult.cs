namespace BudgetBuddy.Application.Common.Models;

public record BatchDeleteResult(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    List<string> Errors);
