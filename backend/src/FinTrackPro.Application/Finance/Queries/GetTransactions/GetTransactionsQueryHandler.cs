using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, IEnumerable<TransactionDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionsQueryHandler(
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var transactions = await _transactionRepository.GetByUserAsync(user.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Month))
            transactions = transactions.Where(t => t.BudgetMonth == request.Month);

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TransactionDto)t);
    }
}
