using BudgetBuddy.API.VSA.Common.Domain.Enums;

namespace BudgetBuddy.API.VSA.Features.Investments.ExportInvestments;

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
