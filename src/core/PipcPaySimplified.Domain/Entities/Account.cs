using PipcPaySimplified.Domain.Entities.User;
using PipcPaySimplified.Domain.Events;
using PipcPaySimplified.Domain.SeedWork;
using PipcPaySimplified.Domain.SeedWork.Interfaces;

namespace PipcPaySimplified.Domain.Entities;

public class Account : Entity
{
    public decimal Balance { get; private set; } = default;

    public virtual UserBase User { get; private set; }

    public Guid UserId { get; private set; }

    private Account() { }

    public Account(UserBase user)
    {
        User = user;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        if (Balance < amount)
        {
            throw new InvalidOperationException();
        }

        Balance -= amount;
    }

    public void TransferTo(Account targetAccount, decimal amount)
    {
        if (User is not IPaymentUser)
        {
            throw new InvalidOperationException();
        }

        targetAccount.Deposit(amount);
        Withdraw(amount);

        RaiseEvent(new TransferReceivedEvent(User.FullName, amount));
    }
}
