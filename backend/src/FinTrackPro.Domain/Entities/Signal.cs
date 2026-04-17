using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Domain.Entities;

public class Signal : CreatedEntity
{
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public SignalType SignalType { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string Timeframe { get; private set; } = string.Empty;
    public bool IsNotified { get; private set; }


    private Signal() { }

    public static Signal Create(
        Guid userId, string symbol, SignalType signalType,
        string message, decimal value, string timeframe)
    {
        return new Signal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = symbol.Trim().ToUpperInvariant(),
            SignalType = signalType,
            Message = message,
            Value = value,
            Timeframe = timeframe,
            IsNotified = false
        };
    }

    public void MarkNotified() => IsNotified = true;
}
