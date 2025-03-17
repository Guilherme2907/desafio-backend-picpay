using Refit;

namespace PipcPaySimplified.Infra.ExternalService.RestEase;

public interface INotifyApi
{
    [Post("/v1/notify")]
    Task NotifyAsync();
}
