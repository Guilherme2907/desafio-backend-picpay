using Refit;

namespace PipcPaySimplified.Infra.ExternalService.RestEase;

public interface ITransferApi
{
    [Get("/v2/authorize")]
    Task AuthorizeAsync();
}
