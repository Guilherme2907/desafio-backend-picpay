using RabbitMQ.Client;

namespace PicPaySimplified.Infra.Messaging.Configurations;

public class ChannelManager
{
    private readonly IConnection _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ChannelManager(IConnection connection)
    {
        _connection = connection;
    }

    public async Task<IChannel> GetChannelAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_channel is null || _channel.IsClosed)
                _channel = await _connection.CreateChannelAsync();

            return _channel;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
