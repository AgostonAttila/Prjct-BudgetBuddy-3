using BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;
using BudgetBuddy.API.VSA.Features.Budgets.UpdateBudget;
using Mapster;
using static BudgetBuddy.API.VSA.Features.Budgets.CreateBudget.CreateBudgetEndpoint;
using static BudgetBuddy.API.VSA.Features.Budgets.UpdateBudget.UpdateBudgetEndpoint;

namespace BudgetBuddy.API.VSA.Features.Budgets;

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
            .Ignore(dest => dest.UpdatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.Category);   // Navigation property

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
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

        // Budget -> BudgetResponse (requires Category to be loaded)
        config.NewConfig<Budget, CreateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty);

        config.NewConfig<Budget, UpdateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty);
    }
}
