using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
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

    // -------------------------------------------------------------------
    // RenewSubscription
    // -------------------------------------------------------------------

    [Fact]
    public void RenewSubscription_Monthly_FreeUser_SetsPlanProAndExpiryNowPlusOneMonth()
    {
        var user = AppUser.Create("free@b.com", "Free");
        var before = DateTime.UtcNow;

        user.RenewSubscription(BillingPeriod.Monthly);

        user.Plan.Should().Be(SubscriptionPlan.Pro);
        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewSubscription_Monthly_ActivePro_ExtendsFromExistingExpiry()
    {
        var user = AppUser.Create("pro@b.com", "Pro");
        var existingExpiry = DateTime.UtcNow.AddMonths(2);
        user.ActivateSubscription("sub_existing", existingExpiry);

        user.RenewSubscription(BillingPeriod.Monthly);

        user.SubscriptionExpiresAt.Should().BeCloseTo(existingExpiry.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewSubscription_Monthly_ExpiredPro_ExtendsFromNow()
    {
        var user = AppUser.Create("expired@b.com", "Expired");
        user.ActivateSubscription("sub_expired", DateTime.UtcNow.AddDays(-1));
        var before = DateTime.UtcNow;

        user.RenewSubscription(BillingPeriod.Monthly);

        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewSubscription_Yearly_FreeUser_SetsPlanProAndExpiryNowPlusOneYear()
    {
        var user = AppUser.Create("free2@b.com", "Free2");
        var before = DateTime.UtcNow;

        user.RenewSubscription(BillingPeriod.Yearly);

        user.Plan.Should().Be(SubscriptionPlan.Pro);
        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddYears(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewSubscription_Yearly_ActivePro_ExtendsFromExistingExpiry()
    {
        var user = AppUser.Create("pro2@b.com", "Pro2");
        var existingExpiry = DateTime.UtcNow.AddMonths(3);
        user.ActivateSubscription("sub_existing2", existingExpiry);

        user.RenewSubscription(BillingPeriod.Yearly);

        user.SubscriptionExpiresAt.Should().BeCloseTo(existingExpiry.AddYears(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenewSubscription_SetsPaymentSubscriptionIdWithBankPrefix()
    {
        var user = AppUser.Create("bank@b.com", "Bank");

        user.RenewSubscription(BillingPeriod.Monthly);

        user.PaymentSubscriptionId.Should().StartWith("bank_");
    }
}
