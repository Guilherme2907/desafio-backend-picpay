using Microsoft.Extensions.Options;
using PicPaySimplified.Infra.Messaging.Configurations;
using PipcPaySimplified.Application.Interfaces;
using RabbitMQ.Client;
using System.Text.Json;

namespace PicPaySimplified.Infra.Messaging.Producer;

public class RabbitMQProducer : IMessageProducer
{
    private readonly ChannelManager _channelManager;
    private readonly string _exchange;

    public RabbitMQProducer(ChannelManager channelManager, IOptions<RabbitMQConfiguration> config)
    {
        _channelManager = channelManager;
        _exchange = config.Value.Exchange!;
    }

    public async Task SendMessage<T>(T message, CancellationToken cancellationToken)
    {
        var channel = await _channelManager.GetChannelAsync();
        var routingKey = EventsMapping.GetRoutingKey<T>();

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await channel.BasicPublishAsync(
            _exchange,
            routingKey,
            body,
            cancellationToken
        );
    }
}
