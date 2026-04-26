using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Admin;

public class AdminGetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<AdminGetUsersQuery, PagedResult<AdminUserDto>>
{
    public async Task<PagedResult<AdminUserDto>> Handle(
        AdminGetUsersQuery request, CancellationToken cancellationToken)
    {
        var allUsers = await userRepository.GetAllAsync(cancellationToken);

        var filtered = string.IsNullOrWhiteSpace(request.EmailFilter)
            ? allUsers
            : allUsers.Where(u => u.Email != null &&
                u.Email.Contains(request.EmailFilter.Trim().ToLowerInvariant(), StringComparison.Ordinal))
              .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => (AdminUserDto)u)
            .ToList();

        return new PagedResult<AdminUserDto>(items, request.Page, request.PageSize, totalCount);
    }
}
