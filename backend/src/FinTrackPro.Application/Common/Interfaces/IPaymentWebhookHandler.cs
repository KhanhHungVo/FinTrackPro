namespace FinTrackPro.Application.Common.Interfaces;

/// <summary>
/// Provider-neutral webhook handler. Accepts the raw request body and all headers
/// so the controller contains zero provider-specific logic.
/// </summary>
public interface IPaymentWebhookHandler
{
    Task<PaymentWebhookResult> HandleAsync(string payload, IDictionary<string, string[]> headers, CancellationToken ct = default);
}

public record PaymentWebhookResult(bool SignatureValid);
