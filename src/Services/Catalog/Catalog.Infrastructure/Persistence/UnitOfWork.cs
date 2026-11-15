using Catalog.Application.Abstractions;

namespace Catalog.Infrastructure.Persistence;

// DbContext уже является Unit of Work: он копит изменения и сохраняет их одной транзакцией.
// Эта обёртка скрывает EF от Application. В блоке A2 здесь же ошибки EF будут переводиться в ошибки приложения
internal sealed class UnitOfWork(CatalogDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await db.SaveChangesAsync(cancellationToken);
}
