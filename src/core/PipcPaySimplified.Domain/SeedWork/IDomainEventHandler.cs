namespace PipcPaySimplified.Domain.SeedWork;

public interface IDomainEventHandler<TDomainEventHandler> 
    where TDomainEventHandler : DomainEvent
{
    Task HandleAsync(TDomainEventHandler domainEvent);
}
