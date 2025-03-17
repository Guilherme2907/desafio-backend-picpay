using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Domain.SeedWork;

namespace PipcPaySimplified.Infra.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly PicPaySimplifiedDbContext _context;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(PicPaySimplifiedDbContext context, IDomainEventPublisher domainEventPublisher)
    {
        _context = context;
        _domainEventPublisher = domainEventPublisher;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            var entities = _context.ChangeTracker
                .Entries<Entity>()
                .Where(entry => entry.Entity.Events.Any())
                .Select(entry => entry.Entity);

            var events = entities.SelectMany(e => e.Events);

            foreach (var @event in events)
                await _domainEventPublisher.PublishAsync((dynamic)@event);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker
                .Entries()
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);

            throw;
        }
    }
}
