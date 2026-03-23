using MediatR;

namespace FinTrackPro.Application.Finance.Commands.UpdateBudget;

public record UpdateBudgetCommand(Guid Id, decimal LimitAmount) : IRequest;
