using Microsoft.EntityFrameworkCore;
using PicPaySimplified.Common.Utilities.Resilience;
using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Domain.Repositories;
using Polly;

namespace PipcPaySimplified.Application.UseCases.MakeTransfer;

public class MakeTransfer : IMakeTransfer
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransferService _transferService;

    public MakeTransfer(
          IAccountRepository accountRepository,
          IUnitOfWork unitOfWork,
          ITransferService transferService
    )
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _transferService = transferService;
    }

    public async Task Handle(MakeTransferInput request, CancellationToken cancellationToken)
    {
        await PollyRetryStrategy.ExecuteAsync(
            async () => await MakeTransferAsync(request),
            options =>
            {
                options.ShouldHandle = new PredicateBuilder()
                        .Handle<DbUpdateConcurrencyException>();
            },
            cancellationToken: cancellationToken
        );
    }

    private async Task MakeTransferAsync(MakeTransferInput request)
    {
        var payerAccount = await _accountRepository.GetById(request.Payer);
        var payeeAccount = await _accountRepository.GetById(request.Payee);

        payerAccount.TransferTo(payeeAccount, request.Value);

        await _accountRepository.Update(payeeAccount);
        await _accountRepository.Update(payerAccount);

        await _transferService.AuthorizeAsync();

        await _unitOfWork.CommitAsync();
    }
}
