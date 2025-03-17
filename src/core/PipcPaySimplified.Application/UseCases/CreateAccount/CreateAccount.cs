using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Domain.Entities;
using PipcPaySimplified.Domain.Factories;
using PipcPaySimplified.Domain.Repositories;
using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Application.UseCases.CreateAccount;

public class CreateAccount : ICreateAccount
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccount(IAccountRepository accountRepository, IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task Handle(CreateAccountInput input, CancellationToken cancellationToken)
    {
        await CheckUserExistsAsync(input);

        var user = UserFactory.CreateUser(
            input.UserType,
            input.FullName,
            input.Password,
            new Cpf(input.Cpf),
            new Email(input.Email)
         );

        var account = new Account(user);

        await _accountRepository.Create(account);

        await _unitOfWork.CommitAsync();
    }

    private async Task CheckUserExistsAsync(CreateAccountInput input)
    {
        var existingUser = await _userRepository.GetByEmailOrCpf(input.Email, input.Cpf);

        if (existingUser is not null)
        {
            if (existingUser.Email.Value.Equals(input.Email))
                throw new InvalidOperationException("Email já existe");

            if (existingUser.Cpf.Value.Equals(input.Cpf))
                throw new InvalidOperationException("CPF já existe");
        }
    }
}
