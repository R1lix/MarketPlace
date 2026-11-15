using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Queries;

// AsNoTracking: данные только читаются, отслеживать изменения не нужно — меньше памяти и быстрее.
// Select в DTO: из базы забираются только нужные колонки, агрегат целиком не собирается.
// Удалённые товары отсекает глобальный фильтр HasQueryFilter
internal sealed class ProductQueries(CatalogDbContext db) : IProductQueries
{
    public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                p.Sku,
                p.CategoryId,
                db.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).Single(),
                p.CreatedAt,
                p.UpdatedAt,
                // По позиции, которую назначает домен (по Id нельзя: Guid v7 в одну миллисекунду идут в случайном порядке)
                p.Images
                    .OrderBy(i => i.Position)
                    .Select(i => new ProductImageDto(i.Id, i.NameImage, i.PathImage))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProductSummaryDto>> GetPageAsync(
        ProductListQuery query, CancellationToken cancellationToken = default)
    {
        // Защита от некорректных параметров, даже если валидатор не вызвали
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, ProductListQuery.MaxPageSize);

        IQueryable<Product> products = db.Products.AsNoTracking();

        if (query.CategoryId is { } categoryId)
            products = products.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE — поиск без учёта регистра. % и _ в тексте поиска экранируются,
            // иначе запрос «%» нашёл бы все товары
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern, @"\") || EF.Functions.ILike(p.Sku, pattern, @"\"));
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            // Стабильный порядок обязателен для пагинации: иначе строки могут «переезжать» между страницами
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.Sku,
                p.CategoryId,
                db.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).Single()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummaryDto>(items, page, pageSize, totalCount);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
