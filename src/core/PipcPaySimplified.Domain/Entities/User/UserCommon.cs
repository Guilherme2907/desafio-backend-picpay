using PipcPaySimplified.Domain.SeedWork.Interfaces;
using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Domain.Entities.User;
public class UserCommon : UserBase, IPaymentUser
{
    private UserCommon()
    { }

    public UserCommon(
        string fullName,
        string password,
        Email email,
        Cpf cpf
    ) : base(fullName, password, email, cpf)
    {
    }
}
