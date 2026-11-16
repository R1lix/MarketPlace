using Catalog.Application.Common;

namespace Catalog.Application.Products;

// Сценарии работы с товарами. Каждый метод — одно действие пользователя (task-based), а не «обнови всё»
public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductSummaryDto>> GetPageAsync(ProductListQuery query, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task AddStockAsync(Guid id, ChangeStockRequest request, CancellationToken cancellationToken = default);

    Task RemoveStockAsync(Guid id, ChangeStockRequest request, CancellationToken cancellationToken = default);

    Task<ProductImageDto> AddImageAsync(Guid id, AddProductImageRequest request, CancellationToken cancellationToken = default);

    Task RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
