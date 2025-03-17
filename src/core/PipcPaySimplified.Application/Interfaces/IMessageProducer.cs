namespace PipcPaySimplified.Application.Interfaces;

public interface IMessageProducer
{
    Task SendMessage<T>(T message, CancellationToken cancellationToken);
}
