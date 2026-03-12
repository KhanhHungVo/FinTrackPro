using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public record GetTransactionsQuery(string? Month = null) : IRequest<IEnumerable<TransactionDto>>;
