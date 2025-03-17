using PipcPaySimplified.Domain.SeedWork;

namespace PipcPaySimplified.Domain.Events;

public class TransferReceivedEvent : DomainEvent
{
    public string Payer {  get; set; }

    public decimal Amount { get; set; }

    public TransferReceivedEvent(string payer, decimal amount) 
    {
        Payer = payer;
        Amount = amount;
    }
}
