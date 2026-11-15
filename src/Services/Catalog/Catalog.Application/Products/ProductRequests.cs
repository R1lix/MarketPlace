namespace Catalog.Application.Products;

// Запросы — то, что приходит от клиента. Формат проверяют валидаторы (блок A5), бизнес-правила — домен

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string Sku);

// Полное изменение карточки (PUT). Остатка и артикула здесь нет: остаток меняется отдельными
// операциями прихода и списания, артикул после создания не меняется
public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId);

// Приход или списание товара
public sealed record ChangeStockRequest(int Quantity);

public sealed record AddProductImageRequest(string Name, string Path);

// Параметры списка товаров: страница, фильтр по категории, поиск по названию и артикулу
public sealed record ProductListQuery(
    int Page = 1,
    int PageSize = ProductListQuery.DefaultPageSize,
    Guid? CategoryId = null,
    string? Search = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;
}
