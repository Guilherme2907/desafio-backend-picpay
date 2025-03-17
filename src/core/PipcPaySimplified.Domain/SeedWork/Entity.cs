using System.ComponentModel.DataAnnotations;

namespace PipcPaySimplified.Domain.SeedWork;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    private readonly IList<DomainEvent> _events = [];

    public IReadOnlyList<DomainEvent> Events => _events.AsReadOnly();

    protected Entity()
      => Id = Guid.NewGuid();

    public void RaiseEvent(DomainEvent e)
    {
        _events.Add(e);
    }

    public void ClearEvents()
    {
        _events.Clear();
    }
}
