using Catalog.Application.Common;
using Catalog.Application.Products;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

// Тонкий слой HTTP: принять запрос → вызвать сервис → вернуть код ответа.
// Ошибки (400/404/409) превращает в ProblemDetails ProblemDetailsExceptionHandler.
// Операции task-based: остаток и картинки меняются отдельными действиями, а не общим PUT
[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Список товаров")]
    [EndpointDescription("Постранично, с фильтром по категории и поиском по названию и артикулу (без учёта регистра).")]
    [ProducesResponseType<PagedResult<ProductSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ProductSummaryDto>> GetPage([FromQuery] ProductListQuery query, CancellationToken cancellationToken) =>
        products.GetPageAsync(query, cancellationToken);

    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    [EndpointSummary("Карточка товара")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProductDto> GetById(Guid id, CancellationToken cancellationToken) =>
        products.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Создать товар")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRoute<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await products.CreateAsync(request, cancellationToken);
        // 201 + заголовок Location с адресом созданного товара
        return TypedResults.CreatedAtRoute(product, GetByIdRoute, new { id = product.Id });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Изменить карточку товара")]
    [EndpointDescription("Название, описание, цена и категория. Остаток и артикул так не меняются.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await products.UpdateAsync(id, request, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpPost("{id:guid}/stock/add")]
    [EndpointSummary("Приход товара на склад")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> AddStock(Guid id, ChangeStockRequest request, CancellationToken cancellationToken)
    {
        await products.AddStockAsync(id, request, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpPost("{id:guid}/stock/remove")]
    [EndpointSummary("Списание товара со склада")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> RemoveStock(Guid id, ChangeStockRequest request, CancellationToken cancellationToken)
    {
        await products.RemoveStockAsync(id, request, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [EndpointSummary("Добавить картинку")]
    [EndpointDescription("Не больше 10 картинок у товара. Location указывает на карточку товара.")]
    [ProducesResponseType<ProductImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRoute<ProductImageDto>> AddImage(Guid id, AddProductImageRequest request, CancellationToken cancellationToken)
    {
        var image = await products.AddImageAsync(id, request, cancellationToken);
        return TypedResults.CreatedAtRoute(image, GetByIdRoute, new { id });
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [EndpointSummary("Удалить картинку")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> RemoveImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await products.RemoveImageAsync(id, imageId, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Удалить товар")]
    [EndpointDescription("Мягкое удаление. Товар с остатком на складе удалить нельзя (400). Повторное удаление — 404.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> Delete(Guid id, CancellationToken cancellationToken)
    {
        await products.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    private const string GetByIdRoute = "GetProductById";
}
