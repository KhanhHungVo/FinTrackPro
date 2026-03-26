using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;

public class GetNotificationPreferenceQueryHandler(
    IUserRepository userRepository,
    INotificationPreferenceRepository preferenceRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetNotificationPreferenceQuery, NotificationPreferenceDto?>
{
    public async Task<NotificationPreferenceDto?> Handle(
        GetNotificationPreferenceQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var pref = await preferenceRepository.GetByUserAsync(user.Id, cancellationToken);
        return pref is null ? null : (NotificationPreferenceDto)pref;
    }
}
