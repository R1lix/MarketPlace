using Catalog.Application.Abstractions;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    // Агрегат загружается целиком: без картинок не сработали бы правила AddImage / RemoveImage.
    // Удалённые товары отсекает глобальный фильтр HasQueryFilter
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products
            .Include(p => p.Images)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Тоже с фильтром удалённых — так же устроен уникальный индекс в базе (WHERE is_deleted = false)
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Sku == sku, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);
}
