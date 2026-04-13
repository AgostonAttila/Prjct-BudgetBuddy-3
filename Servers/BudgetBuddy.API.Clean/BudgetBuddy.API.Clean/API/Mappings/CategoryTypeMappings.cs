using BudgetBuddy.Application.Features.CategoryTypes.CreateCategoryType;
using BudgetBuddy.Application.Features.CategoryTypes.UpdateCategoryType;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class CategoryTypeMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCategoryTypeCommand, CategoryType>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

        config.NewConfig<UpdateCategoryTypeCommand, CategoryType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

        config.NewConfig<CategoryType, CreateCategoryTypeResponse>();
    }
}
