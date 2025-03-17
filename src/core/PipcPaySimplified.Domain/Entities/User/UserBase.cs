using PipcPaySimplified.Domain.SeedWork;
using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Domain.Entities.User;

public abstract class UserBase : Entity
{
    public string FullName { get; protected set; }

    public string Password { get; protected set; }

    public Email Email { get; protected set; }

    public Cpf Cpf { get; protected set; }

    public Account Account { get; protected set; }

    protected UserBase()
    { }

    protected UserBase(string fullName, string password, Email email, Cpf cpf)
    {
        FullName = fullName;
        Password = password;
        Email = email;
        Cpf = cpf;
    }
}
