using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Module.Investments.Features.ExportInvestments;

public record ExportInvestmentsQuery(
    InvestmentType? Type,
    string? Search,
    ExportFormat Format
) : IRequest<ExportInvestmentsResponse>;

public record ExportInvestmentsResponse(
    byte[] FileContent,
    string FileName,
    string ContentType
);
