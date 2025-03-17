using PicPaySimplified.Infra.Messaging.Interfaces;
using PipcPaySimplified.Infra.ExternalService.RestEase;

namespace PipcPaySimplified.Infra.ExternalService.Services;
public class NotifyService : INotifyService
{
    private readonly INotifyApi _notifyApi;

    public NotifyService(INotifyApi notifyApi)
    {
        _notifyApi = notifyApi;
    }

    public async Task NotifyTransferReceivedAsync()
    {
        await _notifyApi.NotifyAsync();
    }
}
