using Microsoft.EntityFrameworkCore;
using PipcPaySimplified.Domain.Entities;
using PipcPaySimplified.Domain.Repositories;

namespace PipcPaySimplified.Infra.Data.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly PicPaySimplifiedDbContext _context;

    public AccountRepository(PicPaySimplifiedDbContext context)
    {
        _context = context;
    }

    public async Task Create(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public async Task<Account> GetById(Guid accountId)
    {
        var account = await _context.Accounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == accountId);

        return account is null ? throw new ArgumentException() : account;
    }

    public async Task Update(Account account)
    {
        _context.Accounts.Update(account);
    }
}
