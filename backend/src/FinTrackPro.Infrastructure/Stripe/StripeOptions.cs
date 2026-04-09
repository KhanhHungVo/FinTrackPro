namespace FinTrackPro.Infrastructure.Stripe;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey     { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
}
