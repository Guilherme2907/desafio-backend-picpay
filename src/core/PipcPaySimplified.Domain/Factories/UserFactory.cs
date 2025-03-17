using PipcPaySimplified.Domain.Entities;
using PipcPaySimplified.Domain.Entities.User;
using PipcPaySimplified.Domain.SeedWork.Enums;
using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Domain.Factories;
public static class UserFactory
{
    public static UserBase CreateUser(UserType userType, string fullName, string password, Cpf cpf, Email email)
    {
        return userType switch
        {
            UserType.Common => new UserCommon(fullName, password, email, cpf),
            UserType.Logistician => new UserLogistician(fullName, password, email, cpf),
            _ => throw new NotImplementedException()
        };
    }
}
