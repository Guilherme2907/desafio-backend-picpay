using PicPaySimplified.Common.Utilities.Resilience;
using PipcPaySimplified.Application.Exceptions;
using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Infra.ExternalService.RestEase;
using Refit;

namespace PipcPaySimplified.Infra.ExternalService.Services;

public class TransferService : ITransferService
{
    private readonly ITransferApi _transferApi;

    public TransferService(ITransferApi transferApi)
    {
        _transferApi = transferApi;
    }

    public async Task AuthorizeAsync()
    {
        try
        {
            await PollyRetryStrategy.ExecuteAsync(_transferApi.AuthorizeAsync);
        }
        catch (ApiException ex)
        {
            throw new TransferAuthorizationFailedException(ex.Message, ex, ex.ReasonPhrase, ex.StatusCode);
        }
    }
}
