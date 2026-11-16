using Catalog.Domain;

namespace Catalog.Application.Products;

// Сущность → DTO. Ручной маппинг: полей немного, а так видно каждое преобразование и его легко проверить
internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product, string categoryName) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.Sku,
        product.CategoryId,
        categoryName,
        product.CreatedAt,
        product.UpdatedAt,
        product.Images.OrderBy(i => i.Position).Select(i => i.ToDto()).ToList());

    public static ProductImageDto ToDto(this ProductImage image) => new(image.Id, image.NameImage, image.PathImage);
}
