using BudgetBuddy.Domain.Enums;

namespace BudgetBuddy.Application.Features.Investments.ExportInvestments;

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
