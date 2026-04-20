using BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.CreateCategoryType;
using BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.UpdateCategoryType;
using Mapster;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes;

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
