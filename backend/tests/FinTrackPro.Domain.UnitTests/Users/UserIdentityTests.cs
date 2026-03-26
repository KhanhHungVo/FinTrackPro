using FinTrackPro.Domain.Entities;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Users;

public class UserIdentityTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var userId = Guid.NewGuid();

        var identity = new UserIdentity("kc-123", "http://keycloak/realm", userId);

        identity.Id.Should().NotBeEmpty();
        identity.ExternalUserId.Should().Be("kc-123");
        identity.Provider.Should().Be("http://keycloak/realm");
        identity.UserId.Should().Be(userId);
    }
}
