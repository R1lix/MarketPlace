namespace Catalog.Application.Products;

// DTO — то, что уходит клиенту. Сущности наружу не отдаём: клиент не должен зависеть
// от внутреннего устройства домена, а лишние поля (IsDeleted, Version) ему не нужны

// Полная карточка товара
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Sku,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProductImageDto> Images);

public sealed record ProductImageDto(Guid Id, string Name, string Path);

// Строка списка товаров — без описания и картинок, чтобы список был лёгким
public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    int Stock,
    string Sku,
    Guid CategoryId,
    string CategoryName);
