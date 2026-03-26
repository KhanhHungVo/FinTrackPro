using System.Security.Claims;
using FinTrackPro.Infrastructure.Identity;
using FluentAssertions;

namespace FinTrackPro.Infrastructure.UnitTests.Identity;

public class ClaimsExtensionsTests
{
    private static ClaimsPrincipal Build(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void GetExternalId_ReturnsSub()
    {
        var principal = Build(new Claim("sub", "user-123"));
        principal.GetExternalId().Should().Be("user-123");
    }

    [Fact]
    public void GetExternalId_MissingClaim_Throws()
    {
        var principal = Build();
        var act = () => principal.GetExternalId();
        act.Should().Throw<InvalidOperationException>().WithMessage("*'sub'*");
    }

    [Fact]
    public void GetProvider_ReturnsIss()
    {
        var principal = Build(new Claim("iss", "http://keycloak/realm"));
        principal.GetProvider().Should().Be("http://keycloak/realm");
    }

    [Fact]
    public void GetProvider_MissingClaim_Throws()
    {
        var principal = Build();
        var act = () => principal.GetProvider();
        act.Should().Throw<InvalidOperationException>().WithMessage("*'iss'*");
    }

    [Fact]
    public void GetEmail_ReturnsEmailClaim()
    {
        var principal = Build(new Claim("email", "test@example.com"));
        principal.GetEmail().Should().Be("test@example.com");
    }

    [Fact]
    public void GetEmail_Missing_ReturnsNull()
    {
        var principal = Build();
        principal.GetEmail().Should().BeNull();
    }

    [Fact]
    public void IsEmailVerified_TrueClaim_ReturnsTrue()
    {
        var principal = Build(new Claim("email_verified", "true"));
        principal.IsEmailVerified().Should().BeTrue();
    }

    [Fact]
    public void IsEmailVerified_MissingOrFalse_ReturnsFalse()
    {
        Build().IsEmailVerified().Should().BeFalse();
        Build(new Claim("email_verified", "false")).IsEmailVerified().Should().BeFalse();
    }
}
