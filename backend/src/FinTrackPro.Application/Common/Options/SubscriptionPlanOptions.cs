namespace FinTrackPro.Application.Common.Options;

public class PlanLimits
{
    public int  MonthlyTransactionLimit      { get; init; }
    public int  TransactionHistoryDays       { get; init; }
    public int  ActiveBudgetLimit            { get; init; }
    public int  TotalTradeLimit              { get; init; }
    public int  WatchlistSymbolLimit         { get; init; }
    public int  SignalHistoryDays            { get; init; }
    public bool TelegramNotificationsEnabled { get; init; }
}

public class SubscriptionPlanOptions
{
    public const string SectionName = "SubscriptionPlans";

    public PlanLimits Free { get; init; } = new();
    public PlanLimits Pro  { get; init; } = new();
}
