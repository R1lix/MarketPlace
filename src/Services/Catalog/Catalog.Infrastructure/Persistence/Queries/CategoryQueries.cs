using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Queries;

internal sealed class CategoryQueries(CatalogDbContext db) : ICategoryQueries
{
    public Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);
}
