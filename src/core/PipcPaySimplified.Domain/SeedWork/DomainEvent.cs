namespace PipcPaySimplified.Domain.SeedWork;

public abstract class DomainEvent
{
    public DateTime OccuerdOn { get; private set; }

    protected DomainEvent()
    {
        OccuerdOn = DateTime.Now;
    }
}
