using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

// Оптимистическая блокировка через xmin: два «запроса» загружают одни данные,
// первый сохраняет, второй должен получить конфликт, а не перезаписать изменения первого
public class ConcurrencyTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ConcurrentPriceChange_SecondSaveFails()
    {
        var (_, product) = await SaveProduct();
        await using var scope1 = Fixture.CreateScope();
        await using var scope2 = Fixture.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var p1 = await db1.Products.SingleAsync(p => p.Id == product.Id);
        var p2 = await db2.Products.SingleAsync(p => p.Id == product.Id);

        p1.ChangePrice(200m);
        await db1.SaveChangesAsync();
        p2.ChangePrice(300m);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        var saved = await InDb(db => db.Products.SingleAsync(p => p.Id == product.Id));
        Assert.Equal(200m, saved.Price);
    }

    // У товара 8 картинок, два запроса одновременно добавляют по две.
    // Каждый в отдельности видит 8 и проверку лимита проходит — без xmin в базе оказалось бы 12
    [Fact]
    public async Task ConcurrentAddImage_CannotExceedImageLimit()
    {
        var (_, product) = await SaveProduct(images: 8);
        await using var scope1 = Fixture.CreateScope();
        await using var scope2 = Fixture.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var p1 = await db1.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);
        var p2 = await db2.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);

        p1.AddImage("a1.png", "/images/a1.png");
        p1.AddImage("a2.png", "/images/a2.png");
        await db1.SaveChangesAsync();
        p2.AddImage("b1.png", "/images/b1.png");
        p2.AddImage("b2.png", "/images/b2.png");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        var count = await InDb(db => db.Set<ProductImage>().CountAsync(i => i.ProductId == product.Id));
        Assert.Equal(Product.MaxImages, count);
    }

    [Fact]
    public async Task ConcurrentCategoryRename_SecondSaveFails()
    {
        var (category, _) = await SaveProduct();
        await using var scope1 = Fixture.CreateScope();
        await using var scope2 = Fixture.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var c1 = await db1.Categories.SingleAsync(c => c.Id == category.Id);
        var c2 = await db2.Categories.SingleAsync(c => c.Id == category.Id);

        c1.Rename("Первое имя");
        await db1.SaveChangesAsync();
        c2.Rename("Второе имя");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }
}
