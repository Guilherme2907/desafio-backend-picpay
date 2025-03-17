using PipcPaySimplified.Domain.Entities;

namespace PipcPaySimplified.Domain.Repositories;

public interface IAccountRepository
{
    Task Create(Account account);

    Task<Account> GetById(Guid accountId);

    Task Update(Account account);
}
