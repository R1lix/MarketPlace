using Catalog.Application.Abstractions;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(CatalogDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    // IgnoreQueryFilters: удалённые товары тоже ссылаются на категорию, база не даст её удалить
    public Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.IgnoreQueryFilters().AnyAsync(p => p.CategoryId == id, cancellationToken);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
