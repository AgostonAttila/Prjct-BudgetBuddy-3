using BudgetBuddy.API.VSA.Features.UserSettings.UpdateUserSettings;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.UserSettings;

public class UpdateUserSettingsValidatorTests
{
    private readonly UpdateUserSettingsValidator _validator = new();

    [Fact]
    public void Validate_WhenPreferredLanguageIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateUserSettingsCommand(null, null, "xx-XX", null, null));
        result.ShouldHaveValidationErrorFor(x => x.PreferredLanguage);
    }

    [Fact]
    public void Validate_WhenDefaultCurrencyIsNotThreeChars_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateUserSettingsCommand(null, null, null, "US", null));
        result.ShouldHaveValidationErrorFor(x => x.DefaultCurrency);
    }

    [Fact]
    public void Validate_WhenDateFormatIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateUserSettingsCommand(null, null, null, null, "MM-dd-yyyy"));
        result.ShouldHaveValidationErrorFor(x => x.DateFormat);
    }

    [Fact]
    public void Validate_WhenAllFieldsAreNull_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateUserSettingsCommand(null, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAllFieldsAreValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateUserSettingsCommand("John", "Doe", "en-US", "USD", "yyyy-MM-dd"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
