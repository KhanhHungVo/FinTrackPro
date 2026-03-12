using FinTrackPro.Domain.Common;
using MediatR;

namespace FinTrackPro.Application.Common.Models;

public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : BaseEvent
{
    public TDomainEvent DomainEvent { get; }
    public DomainEventNotification(TDomainEvent domainEvent) => DomainEvent = domainEvent;
}
