using Microsoft.EntityFrameworkCore;
using PipcPaySimplified.Domain.Entities;
using PipcPaySimplified.Domain.Entities.User;

namespace PipcPaySimplified.Infra.Data;

public class PicPaySimplifiedDbContext : DbContext
{
    public DbSet<UserBase> Users => Set<UserBase>();
    public DbSet<UserCommon> UserCommon => Set<UserCommon>();
    public DbSet<UserLogistician> UserLogistician => Set<UserLogistician>();
    public DbSet<Account> Accounts => Set<Account>();

    public PicPaySimplifiedDbContext(DbContextOptions<PicPaySimplifiedDbContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserBase>().OwnsOne(user => user.Cpf, cpf =>
        {
            cpf.Property(cpf => cpf.Value).HasColumnName("Cpf");
            cpf.HasIndex(cpf => cpf.Value).IsUnique();
        });


        modelBuilder.Entity<UserBase>().OwnsOne(user => user.Email, email =>
        {
            email.Property(email => email.Value).HasColumnName("Email");
            email.HasIndex(email => email.Value).IsUnique();
        });

        modelBuilder.Entity<UserBase>().Ignore(user => user.Events);
        modelBuilder.Entity<Account>().Ignore(account => account.Events);
    }
}
