using BudgetBuddy.Application.Features.Budgets.CreateBudget;
using BudgetBuddy.Application.Features.Budgets.UpdateBudget;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class BudgetMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateBudgetRequest, CreateBudgetCommand>()
            .MapWith(src => new CreateBudgetCommand(
                src.Name,
                src.CategoryId,
                src.Amount,
                src.CurrencyCode,
                src.Year,
                src.Month
            ));

        config.NewConfig<UpdateBudgetRequest, UpdateBudgetCommand>()
            .MapWith(src => new UpdateBudgetCommand(
                Guid.Empty,
                src.Name,
                src.Amount
            ));

        config.NewConfig<CreateBudgetCommand, Budget>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

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

        config.NewConfig<Budget, BudgetBuddy.Application.Features.Budgets.CreateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty);

        config.NewConfig<Budget, BudgetBuddy.Application.Features.Budgets.UpdateBudget.BudgetResponse>()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty);
    }
}
