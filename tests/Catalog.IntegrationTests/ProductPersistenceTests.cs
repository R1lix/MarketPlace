using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests;

// Сохранение и загрузка агрегата Product вместе с картинками
public class ProductPersistenceTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Product_WithImages_IsSavedAndLoaded()
    {
        var (_, product) = await SaveProduct(images: 2, price: 199.99m);

        var loaded = await InDb(db => db.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id));

        Assert.Equal(product.Name, loaded.Name);
        Assert.Equal(product.Description, loaded.Description);
        Assert.Equal(199.99m, loaded.Price);
        Assert.Equal(product.Sku, loaded.Sku);
        Assert.Equal(product.CategoryId, loaded.CategoryId);
        Assert.Equal(2, loaded.Images.Count);
        Assert.All(loaded.Images, i => Assert.Equal(product.Id, i.ProductId));
    }

    [Fact]
    public async Task Dates_AreLoadedAsUtc()
    {
        var (_, product) = await SaveProduct();

        var loaded = await InDb(db => db.Products.SingleAsync(p => p.Id == product.Id));

        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
    }

    // Ловушка, от которой защищает ValueGeneratedNever: картинка с заполненным Id у сохранённого товара
    // должна стать INSERT, а не UPDATE несуществующей строки
    [Fact]
    public async Task AddImage_ToSavedProduct_InsertsNewRow()
    {
        var (_, product) = await SaveProduct(images: 1);

        await InDb(async db =>
        {
            var loaded = await db.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);
            loaded.AddImage("new.png", "/images/new.png");
            await db.SaveChangesAsync();
        });

        var count = await InDb(db => db.Set<ProductImage>().CountAsync(i => i.ProductId == product.Id));
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RemoveImage_DeletesRow()
    {
        var (_, product) = await SaveProduct(images: 3);
        var imageId = product.Images.First().Id;

        await InDb(async db =>
        {
            var loaded = await db.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);
            loaded.RemoveImage(imageId);
            await db.SaveChangesAsync();
        });

        var exists = await InDb(db => db.Set<ProductImage>().AnyAsync(i => i.Id == imageId));
        Assert.False(exists);
    }

    [Fact]
    public async Task ChangedValues_ArePersisted()
    {
        var (_, product) = await SaveProduct(stock: 5);
        var newCategory = new Category("Новая " + Guid.NewGuid().ToString("N")[..8]);

        await InDb(async db =>
        {
            db.Categories.Add(newCategory);
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            loaded.ChangeDetails("Смартфон", "Новое описание");
            loaded.ChangePrice(250.50m);
            loaded.ChangeCategory(newCategory.Id);
            loaded.RemoveStock(2);
            await db.SaveChangesAsync();
        });

        var saved = await InDb(db => db.Products.SingleAsync(p => p.Id == product.Id));
        Assert.Equal("Смартфон", saved.Name);
        Assert.Equal("Новое описание", saved.Description);
        Assert.Equal(250.50m, saved.Price);
        Assert.Equal(newCategory.Id, saved.CategoryId);
        Assert.Equal(3, saved.Stock);
    }
}
