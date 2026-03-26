using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace FinTrackPro.Infrastructure.UnitTests.Identity;

public class UserContextMiddlewareTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly UserContextMiddleware _middleware;
    private bool _nextCalled;

    public UserContextMiddlewareTests()
    {
        _middleware = new UserContextMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Invoke_Authenticated_StoresCurrentUserInHttpContextItems()
    {
        var expectedUser = new CurrentUser(Guid.NewGuid());
        _identityService.ResolveAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        var context = BuildAuthenticatedContext();

        await _middleware.InvokeAsync(context, _identityService);

        context.Items[typeof(ICurrentUser)].Should().Be(expectedUser);
    }

    [Fact]
    public async Task Invoke_Authenticated_CallsNextDelegate()
    {
        _identityService.ResolveAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(new CurrentUser(Guid.NewGuid()));

        await _middleware.InvokeAsync(BuildAuthenticatedContext(), _identityService);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_Unauthenticated_SkipsResolveAsync()
    {
        var context = new DefaultHttpContext();
        // No authenticated identity set

        await _middleware.InvokeAsync(context, _identityService);

        await _identityService.DidNotReceive()
            .ResolveAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>());
        _nextCalled.Should().BeTrue();
    }

    private static DefaultHttpContext BuildAuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([new Claim("sub", "test-user")], "Bearer");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }
}
