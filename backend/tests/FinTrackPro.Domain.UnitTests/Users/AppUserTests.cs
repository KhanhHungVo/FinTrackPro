using FinTrackPro.Domain.Entities;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Users;

public class AppUserTests
{
    [Fact]
    public void Create_ValidArguments_SetsAllFields()
    {
        var user = AppUser.Create("  Test@Example.com  ", "  Test User  ");

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("test@example.com");   // lowercased + trimmed
        user.DisplayName.Should().Be("Test User");    // trimmed
        user.IsActive.Should().BeTrue();
        user.Identities.Should().BeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_NullEmail_StoresEmptyString()
    {
        var user = AppUser.Create(null, "Anonymous");

        user.Email.Should().BeEmpty();
    }

    [Fact]
    public void AddIdentity_NewPair_AddsToCollection()
    {
        var user = AppUser.Create("a@b.com", "Alice");

        user.AddIdentity("kc-1", "http://keycloak/realm");

        user.Identities.Should().HaveCount(1);
        user.Identities.Single().ExternalUserId.Should().Be("kc-1");
        user.Identities.Single().Provider.Should().Be("http://keycloak/realm");
    }

    [Fact]
    public void AddIdentity_DuplicatePair_IsIdempotent()
    {
        var user = AppUser.Create("a@b.com", "Alice");
        user.AddIdentity("kc-1", "http://keycloak/realm");

        user.AddIdentity("kc-1", "http://keycloak/realm");

        user.Identities.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateProfile_ChangedValues_ReturnsTrueAndUpdatesFields()
    {
        var user = AppUser.Create("old@b.com", "Old Name");

        var changed = user.UpdateProfile("NEW@EXAMPLE.COM", "  New Name  ");

        changed.Should().BeTrue();
        user.Email.Should().Be("new@example.com");
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public void UpdateProfile_UnchangedActiveUser_ReturnsFalse()
    {
        var user = AppUser.Create("a@b.com", "Alice");

        var changed = user.UpdateProfile("a@b.com", "Alice");

        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfile_DeactivatedUser_ReactivatesAndReturnsTrue()
    {
        var user = AppUser.Create("a@b.com", "Alice");
        user.Deactivate();

        var changed = user.UpdateProfile("a@b.com", "Alice");

        changed.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = AppUser.Create("a@b.com", "Alice");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }
}
