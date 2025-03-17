using MediatR;

namespace PipcPaySimplified.Application.UseCases.CreateAccount;

public interface ICreateAccount : IRequestHandler<CreateAccountInput>
{
}
