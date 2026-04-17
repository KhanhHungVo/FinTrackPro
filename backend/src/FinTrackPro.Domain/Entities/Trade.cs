using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class Trade : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public TradeDirection Direction { get; private set; }
    public TradeStatus Status { get; private set; } = TradeStatus.Closed;
    public decimal EntryPrice { get; private set; }
    public decimal? ExitPrice { get; private set; }
    public decimal? CurrentPrice { get; private set; }
    public decimal PositionSize { get; private set; }
    public decimal Fees { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal RateToUsd { get; private set; } = 1.0m;
    public string? Notes { get; private set; }


    // Realized P&L for closed trades — not persisted
    public decimal Result => Status == TradeStatus.Closed && ExitPrice.HasValue
        ? (Direction == TradeDirection.Long
            ? (ExitPrice.Value - EntryPrice) * PositionSize - Fees
            : (EntryPrice - ExitPrice.Value) * PositionSize - Fees)
        : 0m;

    // Unrealized P&L for open trades — not persisted
    public decimal? UnrealizedResult => Status == TradeStatus.Open && CurrentPrice.HasValue
        ? (Direction == TradeDirection.Long
            ? (CurrentPrice.Value - EntryPrice) * PositionSize
            : (EntryPrice - CurrentPrice.Value) * PositionSize)
        : null;

    private Trade() { }

    public static Trade Create(
        Guid userId, string symbol, TradeDirection direction, TradeStatus status,
        decimal entryPrice, decimal? exitPrice, decimal? currentPrice,
        decimal positionSize, decimal fees, string currency, decimal rateToUsd, string? notes)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("Symbol is required.");
        if (entryPrice <= 0)
            throw new DomainException("Entry price must be greater than zero.");
        if (status == TradeStatus.Closed && (!exitPrice.HasValue || exitPrice.Value <= 0))
            throw new DomainException("Exit price is required for a closed trade.");
        if (exitPrice.HasValue && exitPrice.Value <= 0)
            throw new DomainException("Exit price must be greater than zero.");
        if (currentPrice.HasValue && currentPrice.Value <= 0)
            throw new DomainException("Current price must be greater than zero.");
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
            Status = status,
            EntryPrice = entryPrice,
            ExitPrice = status == TradeStatus.Closed ? exitPrice : null,
            CurrentPrice = status == TradeStatus.Open ? currentPrice : null,
            PositionSize = positionSize,
            Fees = fees,
            Currency = currency.Trim().ToUpperInvariant(),
            RateToUsd = rateToUsd,
            Notes = notes?.Trim()
        };
    }

    public void Update(
        string symbol, TradeDirection direction, TradeStatus status,
        decimal entryPrice, decimal? exitPrice, decimal? currentPrice,
        decimal positionSize, decimal fees,
        string currency, decimal rateToUsd, string? notes)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("Symbol is required.");
        if (entryPrice <= 0)
            throw new DomainException("Entry price must be greater than zero.");
        if (status == TradeStatus.Closed && (!exitPrice.HasValue || exitPrice.Value <= 0))
            throw new DomainException("Exit price is required for a closed trade.");
        if (exitPrice.HasValue && exitPrice.Value <= 0)
            throw new DomainException("Exit price must be greater than zero.");
        if (currentPrice.HasValue && currentPrice.Value <= 0)
            throw new DomainException("Current price must be greater than zero.");
        if (positionSize <= 0)
            throw new DomainException("Position size must be greater than zero.");
        if (fees < 0)
            throw new DomainException("Fees cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        Symbol = symbol.Trim().ToUpperInvariant();
        Direction = direction;
        Status = status;
        EntryPrice = entryPrice;
        ExitPrice = status == TradeStatus.Closed ? exitPrice : null;
        CurrentPrice = status == TradeStatus.Open ? currentPrice : null;
        PositionSize = positionSize;
        Fees = fees;
        Currency = currency.Trim().ToUpperInvariant();
        RateToUsd = rateToUsd;
        Notes = notes?.Trim();
    }

    public void Close(decimal exitPrice, decimal fees)
    {
        if (Status == TradeStatus.Closed)
            throw new ConflictException("Trade is already closed.");
        if (exitPrice <= 0)
            throw new DomainException("Exit price must be greater than zero.");
        if (fees < 0)
            throw new DomainException("Fees cannot be negative.");

        Status = TradeStatus.Closed;
        ExitPrice = exitPrice;
        CurrentPrice = null;
        Fees = fees;
    }
}
