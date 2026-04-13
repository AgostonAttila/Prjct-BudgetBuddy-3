using BudgetBuddy.Domain.Entities;
using BudgetBuddy.Application.Features.Categories.CreateCategory;
using BudgetBuddy.Application.Features.Categories.UpdateCategory;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class CategoryMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCategoryRequest, CreateCategoryCommand>()
            .MapWith(src => new CreateCategoryCommand(
                src.Name,
                src.Icon,
                src.Color
            ));

        config.NewConfig<UpdateCategoryRequest, UpdateCategoryCommand>()
            .MapWith(src => new UpdateCategoryCommand(
                Guid.Empty,
                src.Name,
                src.Icon,
                src.Color
            ));

        config.NewConfig<CreateCategoryCommand, Category>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Types)
            .Ignore(dest => dest.Transactions);

        config.NewConfig<UpdateCategoryCommand, Category>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Types)
            .Ignore(dest => dest.Transactions);

        config.NewConfig<Category, CreateCategoryResponse>();

        config.NewConfig<Category, BudgetBuddy.Application.Features.Categories.UpdateCategory.CategoryResponse>();
    }
}
