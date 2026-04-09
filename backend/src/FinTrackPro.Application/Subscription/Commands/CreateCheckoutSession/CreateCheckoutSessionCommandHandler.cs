using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Options;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Application.Subscription.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IPaymentGatewayService paymentGateway,
    IOptions<PaymentGatewayOptions> gatewayOptions) : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionDto>
{
    public async Task<CheckoutSessionDto> Handle(
        CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        if (user.PaymentCustomerId is null)
        {
            var customerId = await paymentGateway.CreateCustomerAsync(
                user.Email ?? string.Empty, user.DisplayName, cancellationToken);
            user.SetPaymentCustomerId(customerId);
            // Persist immediately so concurrent requests don't create duplicate customers.
            await context.SaveChangesAsync(cancellationToken);
        }

        var sessionUrl = await paymentGateway.CreateCheckoutSessionAsync(
            user.PaymentCustomerId!,
            gatewayOptions.Value.PriceId,
            request.SuccessUrl,
            request.CancelUrl,
            cancellationToken);

        return new CheckoutSessionDto(sessionUrl);
    }
}
