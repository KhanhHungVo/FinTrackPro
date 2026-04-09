using MediatR;

namespace FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;

public record GetSubscriptionStatusQuery : IRequest<SubscriptionStatusDto>;
