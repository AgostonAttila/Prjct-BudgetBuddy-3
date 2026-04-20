using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Contracts.Accounts;
using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Module.Transactions.Features.Transfers.Services;
using BudgetBuddy.Module.Transactions.Features.Transactions.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetBuddy.Module.Transactions;

public class TransactionsModule : IModule
{
    public string ModuleName => "Transactions";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<TransactionsDbContext>(configuration);
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITransactionValidationService, TransactionValidationService>();
        services.AddScoped<ITransactionSearchService, TransactionSearchService>();
        services.AddScoped<IDataImportService, ExcelImportService>();
        services.AddScoped<IAccountTransactionSummary, AccountTransactionSummaryService>();
        services.AddScoped<ITransactionQueryService, TransactionQueryService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
