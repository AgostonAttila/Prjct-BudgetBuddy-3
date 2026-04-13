using FluentValidation;

namespace BudgetBuddy.Application.Features.UserSettings.UpdateUserSettings;

public class UpdateUserSettingsValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    private static readonly string[] ValidLanguages = { "en-US", "hu-HU", "de-DE", "fr-FR", "es-ES" };
    private static readonly string[] ValidDateFormats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "dd.MM.yyyy" };

    public UpdateUserSettingsValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.PreferredLanguage), () =>
        {
            RuleFor(x => x.PreferredLanguage)
                .Must(lang => ValidLanguages.Contains(lang!))
                .WithMessage($"Language must be one of: {string.Join(", ", ValidLanguages)}");
        });

        When(x => !string.IsNullOrWhiteSpace(x.DefaultCurrency), () =>
        {
            RuleFor(x => x.DefaultCurrency)
                .Length(3)
                .WithMessage("Currency code must be 3 characters (e.g., USD, EUR, GBP)");
        });

        When(x => !string.IsNullOrWhiteSpace(x.DateFormat), () =>
        {
            RuleFor(x => x.DateFormat)
                .Must(format => ValidDateFormats.Contains(format!))
                .WithMessage($"Date format must be one of: {string.Join(", ", ValidDateFormats)}");
        });
    }
}
