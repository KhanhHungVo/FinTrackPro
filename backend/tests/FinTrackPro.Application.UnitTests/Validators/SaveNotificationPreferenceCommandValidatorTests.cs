using FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class SaveNotificationPreferenceCommandValidatorTests
{
    private readonly SaveNotificationPreferenceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new SaveNotificationPreferenceCommand("123456789", true));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTelegramChatId_Fails(string chatId)
    {
        var result = _validator.Validate(new SaveNotificationPreferenceCommand(chatId, true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Telegram chat ID is required.");
    }
}
