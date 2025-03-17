using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Refit;

namespace PicPaySimplified.Common.Utilities.Resilience;

public static class PollyRetryStrategy
{
    public static async Task<T> ExecuteAsync<T>(
           Func<Task<T>> callback,
           Action<RetryStrategyOptions>? configureOptions = null,
           TimeSpan? timeout = null,
           CancellationToken cancellationToken = default)
    {
        var options = GetDefaultRetryOptions();

        configureOptions?.Invoke(options);

        var pipeline = GetPipeline(options, timeout);
        return await pipeline.ExecuteAsync(async token => await callback(), cancellationToken);
    }

    public static async Task ExecuteAsync(
         Func<Task> callback,
         Action<RetryStrategyOptions>? configureOptions = null,
         TimeSpan? timeout = null,
         CancellationToken cancellationToken = default)
    {
        var options = GetDefaultRetryOptions();

        configureOptions?.Invoke(options);

        var pipeline = GetPipeline(options, timeout);
        await pipeline.ExecuteAsync(async token => await callback(), cancellationToken);
    }

    private static RetryStrategyOptions GetDefaultRetryOptions()
    {
        return new RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 6,
            DelayGenerator = (args) =>
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber)); // Exponential backoff
                return new ValueTask<TimeSpan?>(delay);
            },
            OnRetry = args =>
            {
                Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}");
                return ValueTask.CompletedTask;
            }
        };
    }

    private static ResiliencePipeline GetPipeline(RetryStrategyOptions options, TimeSpan? timeout = null)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(options)
            .AddTimeout(timeout ?? TimeSpan.FromSeconds(10))
            .Build();
    }
}

