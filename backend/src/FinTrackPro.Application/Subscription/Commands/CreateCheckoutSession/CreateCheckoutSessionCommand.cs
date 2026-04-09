using MediatR;

namespace FinTrackPro.Application.Subscription.Commands.CreateCheckoutSession;

public record CreateCheckoutSessionCommand(string SuccessUrl, string CancelUrl)
    : IRequest<CheckoutSessionDto>;

public record CheckoutSessionDto(string SessionUrl);
