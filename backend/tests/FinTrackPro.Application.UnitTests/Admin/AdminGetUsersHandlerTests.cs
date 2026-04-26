using FinTrackPro.Application.Admin;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Admin;

public class AdminGetUsersHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly AdminGetUsersQueryHandler _handler;

    public AdminGetUsersHandlerTests()
    {
        _handler = new AdminGetUsersQueryHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_ReturnsPagedListOfUsers()
    {
        var users = Enumerable.Range(1, 5)
            .Select(i => AppUser.Create($"user{i}@dev.com", $"User {i}"))
            .ToList();
        _userRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _handler.Handle(new AdminGetUsersQuery(1, 3, null), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmailFilter_ReturnsOnlyMatchingUsers()
    {
        var users = new List<AppUser>
        {
            AppUser.Create("alice@dev.com", "Alice"),
            AppUser.Create("bob@dev.com", "Bob"),
            AppUser.Create("alice2@dev.com", "Alice2"),
        };
        _userRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _handler.Handle(new AdminGetUsersQuery(1, 20, "alice"), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(u => u.Email.Should().Contain("alice"));
    }
}
