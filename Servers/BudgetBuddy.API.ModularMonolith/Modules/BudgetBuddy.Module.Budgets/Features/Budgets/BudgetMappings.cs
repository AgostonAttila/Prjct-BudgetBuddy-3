using BudgetBuddy.Module.Budgets.Features.CreateBudget;
using BudgetBuddy.Module.Budgets.Features.UpdateBudget;
using Mapster;
using static BudgetBuddy.Module.Budgets.Features.CreateBudget.CreateBudgetEndpoint;
using static BudgetBuddy.Module.Budgets.Features.UpdateBudget.UpdateBudgetEndpoint;

namespace BudgetBuddy.Module.Budgets.Features;

public class BudgetMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateBudgetRequest -> CreateBudgetCommand
        config.NewConfig<CreateBudgetRequest, CreateBudgetCommand>()
            .MapWith(src => new CreateBudgetCommand(
                src.Name,
                src.CategoryId,
                src.Amount,
                src.CurrencyCode,
                src.Year,
                src.Month
            ));

        // UpdateBudgetRequest -> UpdateBudgetCommand
        config.NewConfig<UpdateBudgetRequest, UpdateBudgetCommand>()
            .MapWith(src => new UpdateBudgetCommand(
                Guid.Empty,  
                src.Name,
                src.Amount
            ));

        // CreateBudgetCommand -> Budget
        config.NewConfig<CreateBudgetCommand, Budget>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.UpdatedAt); // Handled by AuditableEntityInterceptor
        // Note: Category navigation property removed (cross-module) — CategoryId FK is mapped directly

        // UpdateBudgetCommand -> Budget (only updating specific fields)
        config.NewConfig<UpdateBudgetCommand, Budget>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Amount, src => src.Amount)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.CurrencyCode)
            .Ignore(dest => dest.Year)
            .Ignore(dest => dest.Month)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        // Budget -> BudgetResponse (Category name not available via navigation — CategoryId only)
        config.NewConfig<Budget, CreateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => string.Empty); // Category name requires cross-module lookup

        config.NewConfig<Budget, UpdateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => string.Empty); // Category name requires cross-module lookup
    }
}
