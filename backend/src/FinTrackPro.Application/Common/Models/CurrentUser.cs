using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Application.Common.Models;

public record CurrentUser(Guid UserId) : ICurrentUser
{
    public static CurrentUser From(AppUser user) => new(user.Id);
}
