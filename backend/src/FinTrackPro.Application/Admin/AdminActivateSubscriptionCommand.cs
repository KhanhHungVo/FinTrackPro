using FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;
using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Admin;

public record AdminActivateSubscriptionCommand(Guid UserId, BillingPeriod Period)
    : IRequest<SubscriptionStatusDto>;
