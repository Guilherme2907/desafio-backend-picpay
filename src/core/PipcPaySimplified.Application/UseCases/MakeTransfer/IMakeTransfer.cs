using MediatR;

namespace PipcPaySimplified.Application.UseCases.MakeTransfer;

public interface IMakeTransfer : IRequestHandler<MakeTransferInput>
{
}
