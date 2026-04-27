using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public record SignalDto(
    Guid Id,
    string Symbol,
    SignalType SignalType,
    string Message,
    decimal Value,
    string Timeframe,
    bool IsNotified,
    DateTime CreatedAt,
    DateTime? DismissedAt)
{
    public static explicit operator SignalDto(Signal s) => new(
        s.Id, s.Symbol, s.SignalType, s.Message, s.Value, s.Timeframe, s.IsNotified, s.CreatedAt, s.DismissedAt);
}
