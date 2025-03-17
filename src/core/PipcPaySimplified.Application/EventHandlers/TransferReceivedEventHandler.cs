using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Domain.Events;
using PipcPaySimplified.Domain.SeedWork;

namespace PipcPaySimplified.Application.EventHandlers;

public class TransferReceivedEventHandler : IDomainEventHandler<TransferReceivedEvent>
{
    private readonly IMessageProducer _messageProducer;

    public TransferReceivedEventHandler(IMessageProducer messageProducer)
    {
        _messageProducer = messageProducer;
    }

    public async Task HandleAsync(TransferReceivedEvent @event)
    {
        await _messageProducer.SendMessage(@event, CancellationToken.None);
    }
}
