namespace FinTrackPro.Application.Finance.DTOs;

public record TransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance);
