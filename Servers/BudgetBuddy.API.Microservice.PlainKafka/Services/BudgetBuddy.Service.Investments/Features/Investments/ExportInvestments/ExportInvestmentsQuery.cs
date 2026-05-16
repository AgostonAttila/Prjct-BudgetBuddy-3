using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Service.Investments.Features.ExportInvestments;

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
