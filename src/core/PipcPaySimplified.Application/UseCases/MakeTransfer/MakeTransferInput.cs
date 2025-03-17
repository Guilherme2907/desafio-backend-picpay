using MediatR;

namespace PipcPaySimplified.Application.UseCases.MakeTransfer;

public record MakeTransferInput(decimal Value, Guid Payer, Guid Payee) : IRequest;

