using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Domain.SeedWork;

namespace PipcPaySimplified.Infra.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly PicPaySimplifiedDbContext _context;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(PicPaySimplifiedDbContext context, IDomainEventPublisher domainEventPublisher, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _domainEventPublisher = domainEventPublisher;
        _logger = logger;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando commit da UnitOfWork.");
            await _context.SaveChangesAsync(cancellationToken);

            var entities = _context.ChangeTracker
                .Entries<Entity>()
                .Where(entry => entry.Entity.Events.Any())
                .Select(entry => entry.Entity);

            var events = entities.SelectMany(e => e.Events);

            foreach (var @event in events)
            {
                _logger.LogInformation("Publicando evento de domínio: {EventType}", @event.GetType().Name);
                await _domainEventPublisher.PublishAsync((dynamic)@event);
            }
            _logger.LogInformation("Commit da UnitOfWork finalizado com sucesso.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Erro de concorrência ao salvar alterações no banco de dados.");
            _context.ChangeTracker
                .Entries()
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao realizar commit da UnitOfWork.");
            throw;
        }
    }
}
