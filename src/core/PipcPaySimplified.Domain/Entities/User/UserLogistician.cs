using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Domain.Entities.User;

public class UserLogistician : UserBase
{
    private UserLogistician()
    { }

    public UserLogistician(
        string fullName,
        string password,
        Email email,
        Cpf cpf
    ) : base(fullName, password, email, cpf)
    {
    }
}
