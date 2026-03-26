using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;

public class SaveNotificationPreferenceCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    INotificationPreferenceRepository preferenceRepository) : IRequestHandler<SaveNotificationPreferenceCommand>
{
    public async Task Handle(SaveNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var existing = await preferenceRepository.GetByUserAsync(user.Id, cancellationToken);
        if (existing is null)
        {
            var preference = NotificationPreference.CreateTelegram(user.Id, request.TelegramChatId);
            if (!request.IsEnabled) preference.Update(request.TelegramChatId, false);
            preferenceRepository.Add(preference);
        }
        else
        {
            existing.Update(request.TelegramChatId, request.IsEnabled);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
