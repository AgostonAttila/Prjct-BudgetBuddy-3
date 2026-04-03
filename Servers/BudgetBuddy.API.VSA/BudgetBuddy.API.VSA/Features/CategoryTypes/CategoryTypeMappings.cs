using BudgetBuddy.API.VSA.Features.CategoryTypes.CreateCategoryType;
using BudgetBuddy.API.VSA.Features.CategoryTypes.UpdateCategoryType;
using Mapster;

namespace BudgetBuddy.API.VSA.Features.CategoryTypes;

public class CategoryTypeMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateCategoryTypeCommand -> CategoryType
        config.NewConfig<CreateCategoryTypeCommand, CategoryType>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Ignore(dest => dest.CreatedAt)     // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.UpdatedAt)     // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.Category);     // Navigation property

        // UpdateCategoryTypeCommand -> CategoryType
        config.NewConfig<UpdateCategoryTypeCommand, CategoryType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

        // CategoryType -> CreateCategoryTypeResponse
        config.NewConfig<CategoryType, CreateCategoryTypeResponse>();
    }
}
