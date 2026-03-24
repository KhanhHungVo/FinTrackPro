using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;

public class GetNotificationPreferenceQueryHandler(
    IUserRepository userRepository,
    INotificationPreferenceRepository preferenceRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationPreferenceQuery, NotificationPreferenceDto?>
{
    public async Task<NotificationPreferenceDto?> Handle(
        GetNotificationPreferenceQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var pref = await preferenceRepository.GetByUserAsync(user.Id, cancellationToken);
        return pref is null ? null : (NotificationPreferenceDto)pref;
    }
}
