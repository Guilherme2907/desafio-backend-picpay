using Microsoft.EntityFrameworkCore;
using PipcPaySimplified.Domain.Entities.User;
using PipcPaySimplified.Domain.Repositories;
using PipcPaySimplified.Domain.ValueObjects;

namespace PipcPaySimplified.Infra.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PicPaySimplifiedDbContext _context;

    public UserRepository(PicPaySimplifiedDbContext context)
    {
        _context = context;
    }

    public async Task<UserBase?> GetByEmailOrCpf(string email, string cpf)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.Value.Equals(email) || u.Cpf.Value.Equals(cpf));
    }
}
