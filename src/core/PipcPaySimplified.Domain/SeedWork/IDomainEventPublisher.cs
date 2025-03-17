namespace PipcPaySimplified.Domain.SeedWork;

public interface IDomainEventPublisher
{
    Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent;
}
