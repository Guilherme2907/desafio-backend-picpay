using PipcPaySimplified.Domain.Entities;
using PipcPaySimplified.Domain.Entities.User;

namespace PipcPaySimplified.Domain.Repositories;

public interface IUserRepository
{
    Task<UserBase?> GetByEmailOrCpf(string email, string cpf);
}
