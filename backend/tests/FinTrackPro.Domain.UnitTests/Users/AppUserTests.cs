using FinTrackPro.Domain.Entities;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Users;

public class AppUserTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsAppUser()
    {
        var user = AppUser.Create("kc-123", "Test@Example.com", "  Test User  ", "local");

        user.Id.Should().NotBeEmpty();
        user.KeycloakUserId.Should().Be("kc-123");
        user.Email.Should().Be("test@example.com");    // lowercased
        user.DisplayName.Should().Be("Test User");     // trimmed
        user.Provider.Should().Be("local");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = AppUser.Create("kc-1", "a@b.com", "Alice", "local");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_SetsIsActiveToTrue()
    {
        var user = AppUser.Create("kc-1", "a@b.com", "Alice", "local");
        user.Deactivate();

        user.Reactivate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateProfile_ChangesDisplayNameAndEmail()
    {
        var user = AppUser.Create("kc-1", "old@b.com", "Old Name", "local");

        user.UpdateProfile("  New Name  ", "NEW@EXAMPLE.COM");

        user.DisplayName.Should().Be("New Name");
        user.Email.Should().Be("new@example.com");
    }
}
