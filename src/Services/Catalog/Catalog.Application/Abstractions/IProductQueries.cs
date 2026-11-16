using Catalog.Application.Common;
using Catalog.Application.Products;

namespace Catalog.Application.Abstractions;

// Чтение товаров для отображения: сразу в DTO, без загрузки агрегата и отслеживания изменений.
// Изменения идут через IProductRepository — чтение и запись разделены (CQRS «на минималках»)
public interface IProductQueries
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductSummaryDto>> GetPageAsync(ProductListQuery query, CancellationToken cancellationToken = default);
}
