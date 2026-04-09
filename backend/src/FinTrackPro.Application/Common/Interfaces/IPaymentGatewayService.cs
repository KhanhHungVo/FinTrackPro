namespace FinTrackPro.Application.Common.Interfaces;

/// <summary>
/// Provider-neutral payment gateway abstraction.
/// All provider-specific details live in the Infrastructure implementation.
/// </summary>
public interface IPaymentGatewayService
{
    Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct = default);
    Task<string> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string> CreateBillingPortalSessionAsync(string customerId, string returnUrl, CancellationToken ct = default);
}
