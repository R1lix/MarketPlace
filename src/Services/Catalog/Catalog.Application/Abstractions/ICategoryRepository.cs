using Catalog.Domain;

namespace Catalog.Application.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // Есть ли в категории товары (включая удалённые: на них тоже ссылается внешний ключ).
    // Позволяет ответить понятной ошибкой до того, как база отклонит удаление
    Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Remove(Category category);
}
