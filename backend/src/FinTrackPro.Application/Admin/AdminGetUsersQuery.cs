using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Admin;

public record AdminGetUsersQuery(int Page, int PageSize, string? EmailFilter) : IRequest<PagedResult<AdminUserDto>>;
