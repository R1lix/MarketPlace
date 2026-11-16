using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests;

// Правила, которые обеспечивает сама база: уникальный индекс, внешние ключи, каскад, фильтр удалённых
public class DatabaseConstraintsTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DuplicateSku_OfActiveProducts_IsRejected()
    {
        var (category, product) = await SaveProduct();

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => InDb(async db =>
        {
            db.Products.Add(new Product("Дубль", null, 1m, 0, category.Id, product.Sku));
            await db.SaveChangesAsync();
        }));

        Assert.Equal("23505", SqlState(exception));
    }

    [Fact]
    public async Task Sku_OfDeletedProduct_CanBeReused()
    {
        var (category, product) = await SaveProduct();
        await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            loaded.Delete();
            await db.SaveChangesAsync();
        });

        var newProduct = new Product("Новый", null, 1m, 0, category.Id, product.Sku);
        await InDb(async db =>
        {
            db.Products.Add(newProduct);
            await db.SaveChangesAsync();
        });

        Assert.True(await InDb(db => db.Products.AnyAsync(p => p.Id == newProduct.Id)));
    }

    [Fact]
    public async Task DeletedProduct_IsHiddenByQueryFilter()
    {
        var (_, product) = await SaveProduct();

        await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            loaded.Delete();
            await db.SaveChangesAsync();
        });

        Assert.False(await InDb(db => db.Products.AnyAsync(p => p.Id == product.Id)));

        var raw = await InDb(db => db.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == product.Id));
        Assert.True(raw.IsDeleted);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task Category_WithProducts_CannotBeDeleted()
    {
        var (category, _) = await SaveProduct();

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => InDb(async db =>
        {
            var loaded = await db.Categories.SingleAsync(c => c.Id == category.Id);
            db.Categories.Remove(loaded);
            await db.SaveChangesAsync();
        }));

        Assert.Equal("23503", SqlState(exception));
    }

    [Fact]
    public async Task PhysicalDeleteOfProduct_DeletesItsImages()
    {
        var (_, product) = await SaveProduct(images: 3);

        await InDb(db => db.Products.IgnoreQueryFilters().Where(p => p.Id == product.Id).ExecuteDeleteAsync());

        var images = await InDb(db => db.Set<ProductImage>().CountAsync(i => i.ProductId == product.Id));
        Assert.Equal(0, images);
    }
}
