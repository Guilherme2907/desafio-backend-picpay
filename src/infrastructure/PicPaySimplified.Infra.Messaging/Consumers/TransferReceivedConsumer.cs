using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PicPaySimplified.Infra.Messaging.Configurations;
using PicPaySimplified.Infra.Messaging.Interfaces;
using PipcPaySimplified.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PicPaySimplified.Infra.Messaging.Consumers;

public class TransferReceivedConsumer : BackgroundService
{
    private readonly ChannelManager _channelManager;
    private readonly string _queue;
    private readonly string _exchange;
    private const string ROUTING_KEY = "transfer.received";
    private IChannel _channel;
    private readonly INotifyService _notifyService;
    private readonly IServiceScopeFactory _scopeFactory;

    public TransferReceivedConsumer(ChannelManager channelManager, IOptions<RabbitMQConfiguration> config, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        using var scope = scopeFactory.CreateScope();

        _channelManager = channelManager;
        _queue = config.Value.TransferReceivedQueue!;
        _exchange = config.Value.Exchange!;
        _notifyService = scope.ServiceProvider.GetRequiredService<INotifyService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _channelManager.GetChannelAsync();

        await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Direct, cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(_queue, true, autoDelete: false, cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(queue: _queue, exchange: _exchange, routingKey: ROUTING_KEY);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += SendNotificationTransferReceived;

        await _channel.BasicConsumeAsync(_queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
    }

    private async Task SendNotificationTransferReceived(object sender, BasicDeliverEventArgs @event)
    {
        var stringMessage = Encoding.UTF8.GetString(@event.Body.ToArray());
        var message = JsonSerializer.Deserialize<TransferReceivedEvent>(stringMessage);

        try
        {
            await _notifyService.NotifyTransferReceivedAsync();

            await _channel.BasicAckAsync(@event.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            await _channel.BasicNackAsync(@event.DeliveryTag, false, true);
        }
    }
}
