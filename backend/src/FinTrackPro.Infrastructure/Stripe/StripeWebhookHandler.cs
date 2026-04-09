using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTrackPro.Infrastructure.Stripe;

public class StripeWebhookHandler(
    IUserRepository userRepository,
    IApplicationDbContext context,
    IOptions<StripeOptions> options,
    ILogger<StripeWebhookHandler> logger) : IPaymentWebhookHandler
{
    public async Task<PaymentWebhookResult> HandleAsync(
        string payload, IDictionary<string, string[]> headers, CancellationToken ct = default)
    {
        // "Stripe-Signature" is Stripe-specific — it lives here, not in the controller.
        headers.TryGetValue("Stripe-Signature", out var sigValues);
        var signature = sigValues?.FirstOrDefault() ?? string.Empty;

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, options.Value.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return new PaymentWebhookResult(SignatureValid: false);
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CustomerSubscriptionUpdated:
            case EventTypes.InvoicePaymentSucceeded:
                await HandleActivationAsync(stripeEvent, ct);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
            case EventTypes.InvoicePaymentFailed:
                await HandleCancellationAsync(stripeEvent, ct);
                break;

            default:
                logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                break;
        }

        return new PaymentWebhookResult(SignatureValid: true);
    }

    private async Task HandleActivationAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription subscription) return;

        var user = await userRepository.GetByPaymentCustomerIdAsync(subscription.CustomerId, ct);
        if (user is null)
        {
            logger.LogWarning("Stripe activation event for unknown customer {CustomerId}", subscription.CustomerId);
            return;
        }

        // In Stripe.net v51+, period end is on SubscriptionItem, not the Subscription root.
        var expiresAt = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd
            ?? DateTime.UtcNow.AddMonths(1);
        user.ActivateSubscription(subscription.Id, expiresAt);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} activated Pro via Stripe subscription {SubId}", user.Id, subscription.Id);
    }

    private async Task HandleCancellationAsync(Event stripeEvent, CancellationToken ct)
    {
        string? customerId = stripeEvent.Data.Object switch
        {
            Subscription sub => sub.CustomerId,
            Invoice inv      => inv.CustomerId,
            _                => null
        };

        if (customerId is null) return;

        var user = await userRepository.GetByPaymentCustomerIdAsync(customerId, ct);
        if (user is null)
        {
            logger.LogWarning("Stripe cancellation event for unknown customer {CustomerId}", customerId);
            return;
        }

        user.CancelSubscription();
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} reverted to Free via Stripe cancellation event", user.Id);
    }
}
