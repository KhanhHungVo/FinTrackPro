using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class Trade : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public TradeDirection Direction { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal ExitPrice { get; private set; }
    public decimal PositionSize { get; private set; }
    public decimal Fees { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal RateToUsd { get; private set; } = 1.0m;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Calculated P&L — not persisted
    public decimal Result => (ExitPrice - EntryPrice) * PositionSize - Fees;

    private Trade() { }

    public static Trade Create(
        Guid userId, string symbol, TradeDirection direction,
        decimal entryPrice, decimal exitPrice, decimal positionSize,
        decimal fees, string currency, decimal rateToUsd, string? notes)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("Symbol is required.");
        if (entryPrice <= 0)
            throw new DomainException("Entry price must be greater than zero.");
        if (exitPrice <= 0)
            throw new DomainException("Exit price must be greater than zero.");
        if (positionSize <= 0)
            throw new DomainException("Position size must be greater than zero.");
        if (fees < 0)
            throw new DomainException("Fees cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        return new Trade
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = symbol.Trim().ToUpperInvariant(),
            Direction = direction,
            EntryPrice = entryPrice,
            ExitPrice = exitPrice,
            PositionSize = positionSize,
            Fees = fees,
            Currency = currency.Trim().ToUpperInvariant(),
            RateToUsd = rateToUsd,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string symbol, TradeDirection direction,
        decimal entryPrice, decimal exitPrice,
        decimal positionSize, decimal fees,
        string currency, decimal rateToUsd, string? notes)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("Symbol is required.");
        if (entryPrice <= 0)
            throw new DomainException("Entry price must be greater than zero.");
        if (exitPrice <= 0)
            throw new DomainException("Exit price must be greater than zero.");
        if (positionSize <= 0)
            throw new DomainException("Position size must be greater than zero.");
        if (fees < 0)
            throw new DomainException("Fees cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        Symbol = symbol.Trim().ToUpperInvariant();
        Direction = direction;
        EntryPrice = entryPrice;
        ExitPrice = exitPrice;
        PositionSize = positionSize;
        Fees = fees;
        Currency = currency.Trim().ToUpperInvariant();
        RateToUsd = rateToUsd;
        Notes = notes?.Trim();
    }
}
