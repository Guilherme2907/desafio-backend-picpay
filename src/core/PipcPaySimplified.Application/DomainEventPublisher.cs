using Microsoft.Extensions.DependencyInjection;
using PipcPaySimplified.Domain.SeedWork;

namespace PipcPaySimplified.Application;

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();

        if (!handlers.Any())
            return;

        foreach (var handler in handlers)
            await handler.HandleAsync(domainEvent);
    }
}
