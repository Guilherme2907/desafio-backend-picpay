using Microsoft.Extensions.Options;
using PicPaySimplified.Common.Utilities.Resilience;
using PicPaySimplified.Infra.Messaging.Configurations;
using RabbitMQ.Client;

namespace PipcPaySimplified.Api.Configurations;

public static class RabbitMqConfiguration
{
    public static IServiceCollection AddRabbitMqConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<RabbitMQConfiguration>>().Value;

            var factory = new ConnectionFactory
            {
                HostName = config.Hostname!,
                Port = config.Port,
                UserName = config.Username!,
                Password = config.Password!

            };

            return Task.Run(() => CreateConnectionAsync(factory)).GetAwaiter().GetResult();
        });

        return services;
    }

    private static async Task<IConnection> CreateConnectionAsync(ConnectionFactory factory)
    {
        return await PollyRetryStrategy.ExecuteAsync(
                async () => await factory.CreateConnectionAsync(),
                configureOptions: options =>
                {
                    options.Delay = TimeSpan.FromSeconds(5);
                    options.MaxRetryAttempts = 10;
                    options.OnRetry = args =>
                    {
                        Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}");
                        return ValueTask.CompletedTask;
                    };
                }
            );
    }
}
