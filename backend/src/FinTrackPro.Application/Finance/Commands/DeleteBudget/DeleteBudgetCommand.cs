using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteBudget;

public record DeleteBudgetCommand(Guid Id) : IRequest;
