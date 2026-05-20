using BudgetBuddy.Service.ReferenceData.Features.Categories.CreateCategory;
using BudgetBuddy.Service.ReferenceData.Features.Categories.UpdateCategory;
using Mapster;
using static BudgetBuddy.Service.ReferenceData.Features.Categories.CreateCategory.CreateCategoryEndpoint;
using static BudgetBuddy.Service.ReferenceData.Features.Categories.UpdateCategory.UpdateCategoryEndpoint;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories;

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
            .Ignore(dest => dest.Types);

        // UpdateCategoryCommand -> Category
        config.NewConfig<UpdateCategoryCommand, Category>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Types);

        // Category -> CreateCategoryResponse
        config.NewConfig<Category, CreateCategoryResponse>();

        // Category -> CategoryResponse (UpdateCategory response)
        config.NewConfig<Category, UpdateCategory.CategoryResponse>();
    }
}
