using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.Categories.CreateCategory;
using BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;
using Mapster;
using static BudgetBuddy.API.VSA.Features.Categories.CreateCategory.CreateCategoryEndpoint;
using static BudgetBuddy.API.VSA.Features.Categories.UpdateCategory.UpdateCategoryEndpoint;

namespace BudgetBuddy.API.VSA.Features.Categories;

public class CategoryMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateCategoryRequest -> CreateCategoryCommand
        config.NewConfig<CreateCategoryRequest, CreateCategoryCommand>()
            .MapWith(src => new CreateCategoryCommand(
                src.Name,
                src.Icon,
                src.Color
            ));

        // UpdateCategoryRequest -> UpdateCategoryCommand
        config.NewConfig<UpdateCategoryRequest, UpdateCategoryCommand>()
            .MapWith(src => new UpdateCategoryCommand(
                Guid.Empty,  
                src.Name,
                src.Icon,
                src.Color
            ));

        // CreateCategoryCommand -> Category
        config.NewConfig<CreateCategoryCommand, Category>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Ignore(dest => dest.CreatedAt)     // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.User)          // Navigation property
            .Ignore(dest => dest.Types)         // Navigation property
            .Ignore(dest => dest.Transactions); // Navigation property

        // UpdateCategoryCommand -> Category
        config.NewConfig<UpdateCategoryCommand, Category>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Types)
            .Ignore(dest => dest.Transactions);

        // Category -> CreateCategoryResponse
        config.NewConfig<Category, CreateCategoryResponse>();

        // Category -> CategoryResponse (UpdateCategory response)
        config.NewConfig<Category, UpdateCategory.CategoryResponse>();
    }
}
