using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using global::Stripe;
using BillingPortalSessionService = global::Stripe.BillingPortal.SessionService;
using CheckoutSessionService = global::Stripe.Checkout.SessionService;
using BillingPortalSessionCreateOptions = global::Stripe.BillingPortal.SessionCreateOptions;
using CheckoutSessionCreateOptions = global::Stripe.Checkout.SessionCreateOptions;

namespace FinTrackPro.Infrastructure.Stripe;

public class StripePaymentGatewayService(IOptions<StripeOptions> options) : IPaymentGatewayService
{
    private CustomerService             CustomerSvc => new(new StripeClient(options.Value.SecretKey));
    private CheckoutSessionService      SessionSvc  => new(new StripeClient(options.Value.SecretKey));
    private BillingPortalSessionService PortalSvc   => new(new StripeClient(options.Value.SecretKey));

    public async Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct = default)
    {
        var customer = await CustomerSvc.CreateAsync(
            new CustomerCreateOptions { Email = email, Name = name },
            cancellationToken: ct);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string customerId, string priceId, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var session = await SessionSvc.CreateAsync(new CheckoutSessionCreateOptions
        {
            Customer           = customerId,
            Mode               = "subscription",
            LineItems          = [new() { Price = priceId, Quantity = 1 }],
            SuccessUrl         = successUrl,
            CancelUrl          = cancelUrl,
            AllowPromotionCodes = true,
        }, cancellationToken: ct);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(
        string customerId, string returnUrl, CancellationToken ct = default)
    {
        var session = await PortalSvc.CreateAsync(new BillingPortalSessionCreateOptions
        {
            Customer  = customerId,
            ReturnUrl = returnUrl,
        }, cancellationToken: ct);
        return session.Url;
    }
}
