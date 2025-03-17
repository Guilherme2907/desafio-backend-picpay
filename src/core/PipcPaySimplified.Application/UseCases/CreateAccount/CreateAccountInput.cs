using MediatR;
using PipcPaySimplified.Domain.SeedWork.Enums;

namespace PipcPaySimplified.Application.UseCases.CreateAccount;

public record CreateAccountInput(
    string FullName,
    string Password,
    string Cpf,
    string Email,
    UserType UserType
) : IRequest;

