using Catalog.Domain;

namespace Catalog.Application.Abstractions;

// Доступ к агрегату Product для изменений: загрузить целиком (с картинками) и добавить новый.
// Удалённые товары не возвращаются (мягкое удаление). Списки для чтения — в отдельном IProductQueries (блок A3).
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Занят ли артикул неудалённым товаром. Артикул передаётся уже нормализованным (как в Product.Sku)
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    void Add(Product product);
}
