namespace FinTrackPro.Application.Common.Options;

public class PaymentGatewayOptions
{
    public const string SectionName = "PaymentGateway";

    public string Provider { get; init; } = "stripe";
    public string PriceId  { get; init; } = "";
}
