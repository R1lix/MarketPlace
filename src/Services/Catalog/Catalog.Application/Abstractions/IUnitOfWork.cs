namespace Catalog.Application.Abstractions;

// Сохраняет все изменения одного сценария одной транзакцией.
// Репозитории только собирают изменения, записываются они здесь — один раз в конце сценария
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
