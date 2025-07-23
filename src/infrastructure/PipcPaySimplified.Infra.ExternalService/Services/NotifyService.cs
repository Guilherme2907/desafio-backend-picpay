using Microsoft.Extensions.Logging;
using PicPaySimplified.Infra.Messaging.Interfaces;
using PipcPaySimplified.Infra.ExternalService.RestEase;

namespace PipcPaySimplified.Infra.ExternalService.Services;
public class NotifyService : INotifyService
{
    private readonly INotifyApi _notifyApi;
    private readonly ILogger<NotifyService> _logger;

    public NotifyService(INotifyApi notifyApi, ILogger<NotifyService> logger)
    {
        _notifyApi = notifyApi;
        _logger = logger;
    }

    public async Task NotifyTransferReceivedAsync()
    {
        _logger.LogInformation("Iniciando notificação de transferência recebida.");
        try
        {
            await _notifyApi.NotifyAsync();
            _logger.LogInformation("Notificação de transferência enviada com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar transferência recebida.");
            throw;
        }
    }
}
