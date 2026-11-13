namespace Catalog.UnitTests;

// Создание тестовых объектов с корректными значениями по умолчанию.
// В тесте меняется только то, что важно для конкретного сценария.
internal static class TestData
{
    public static Product Product(
        string name = "Телефон",
        string? description = null,
        decimal price = 100m,
        int stock = 0,
        Guid? categoryId = null,
        string sku = "AB-1") =>
        new(name, description, price, stock, categoryId ?? Guid.NewGuid(), sku);

    public static Product DeletedProduct()
    {
        var product = Product(stock: 0);
        product.Delete();
        return product;
    }

    public static Product ProductWithImages(int count)
    {
        var product = Product();
        for (var i = 0; i < count; i++)
            product.AddImage($"img{i}.png", $"/images/{i}.png");
        return product;
    }
}
