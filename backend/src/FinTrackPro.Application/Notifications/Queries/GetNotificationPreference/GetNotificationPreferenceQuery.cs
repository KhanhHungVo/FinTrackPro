using MediatR;

namespace FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;

public record GetNotificationPreferenceQuery : IRequest<NotificationPreferenceDto?>;
