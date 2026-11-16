using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Exceptions;
using Catalog.Domain;

namespace Catalog.Application.Products;

// Оркестрация: загрузить агрегат → вызвать метод домена (он проверяет правила) → сохранить.
// Здесь только то, чего домен сам знать не может: существует ли категория, свободен ли артикул.
// Ошибки: NotFoundException → 404, ValidationException / DomainException → 400, ConflictException → 409
internal sealed class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IProductQueries queries,
    IUnitOfWork unitOfWork) : IProductService
{
    private const string ProductEntity = "Товар";

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new ValidationException(nameof(request.CategoryId), "Категория не существует");

        // Сначала создаём товар: домен проверит данные и нормализует артикул (Trim + верхний регистр),
        // и уникальность проверяется уже по нормализованному значению — « ab-1 » и «AB-1» один артикул
        var product = new Product(request.Name, request.Description, request.Price, request.Stock, category.Id, request.Sku);

        if (await products.SkuExistsAsync(product.Sku, cancellationToken))
            throw new ConflictException($"Товар с артикулом {product.Sku} уже существует");

        products.Add(product);
        // Если между проверкой и сохранением артикул займёт другой запрос, его поймает уникальный индекс,
        // а UnitOfWork превратит ошибку в ConflictException
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(category.Name);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await queries.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(ProductEntity, id);

    public Task<PagedResult<ProductSummaryDto>> GetPageAsync(ProductListQuery query, CancellationToken cancellationToken = default) =>
        queries.GetPageAsync(query, cancellationToken);

    public async Task UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);

        // Существование категории проверяем, только если её меняют
        if (request.CategoryId != product.CategoryId
            && !await categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new ValidationException(nameof(request.CategoryId), "Категория не существует");

        product.ChangeDetails(request.Name, request.Description);
        product.ChangePrice(request.Price);
        product.ChangeCategory(request.CategoryId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStockAsync(Guid id, ChangeStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);
        product.AddStock(request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveStockAsync(Guid id, ChangeStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);
        product.RemoveStock(request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductImageDto> AddImageAsync(Guid id, AddProductImageRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);
        var image = product.AddImage(request.Name, request.Path);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return image.ToDto();
    }

    public async Task RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);

        // Id картинки приходит в адресе запроса, поэтому её отсутствие — это 404, а не ошибка бизнес-правила
        if (product.Images.All(i => i.Id != imageId))
            throw new NotFoundException("Картинка", imageId);

        product.RemoveImage(imageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Повторное удаление вернёт 404: удалённый товар скрыт фильтром. Состояние системы при этом
    // не меняется, поэтому DELETE остаётся идемпотентным
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken);
        product.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        await products.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(ProductEntity, id);
}
