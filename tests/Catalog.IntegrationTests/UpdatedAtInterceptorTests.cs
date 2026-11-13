using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests;

// UpdatedAt ставит UpdatedAtInterceptor при сохранении, время берётся из TimeProvider
public class UpdatedAtInterceptorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task NewProduct_HasNoUpdatedAt()
    {
        var (_, product) = await SaveProduct();

        var loaded = await InDb(db => db.Products.SingleAsync(p => p.Id == product.Id));

        Assert.Null(loaded.UpdatedAt);
    }

    [Fact]
    public async Task ChangedProduct_GetsUpdatedAtFromTimeProvider()
    {
        var (_, product) = await SaveProduct();
        Fixture.Time.Advance(TimeSpan.FromMinutes(5));
        var expected = Fixture.Time.GetUtcNow().UtcDateTime;

        await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            loaded.ChangePrice(150m);
            await db.SaveChangesAsync();
        });

        var saved = await InDb(db => db.Products.SingleAsync(p => p.Id == product.Id));
        Assert.Equal(expected, saved.UpdatedAt);
    }

    // Изменились только картинки: строка товара сама по себе не меняется,
    // но interceptor должен «тронуть» товар — обновить UpdatedAt и xmin
    [Fact]
    public async Task ImagesOnlyChange_TouchesProduct()
    {
        var (_, product) = await SaveProduct(images: 2);
        var versionBefore = await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            return db.Entry(loaded).Property<uint>("Version").CurrentValue;
        });
        Fixture.Time.Advance(TimeSpan.FromMinutes(5));
        var expected = Fixture.Time.GetUtcNow().UtcDateTime;

        await InDb(async db =>
        {
            var loaded = await db.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);
            loaded.RemoveImage(loaded.Images.First().Id);
            await db.SaveChangesAsync();
        });

        var (updatedAt, versionAfter) = await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            return (loaded.UpdatedAt, db.Entry(loaded).Property<uint>("Version").CurrentValue);
        });
        Assert.Equal(expected, updatedAt);
        Assert.NotEqual(versionBefore, versionAfter);
    }

    // Время не изменилось (те же «замороженные» часы), но строка всё равно должна обновиться,
    // иначе проверка xmin не сработает. Это дефект, найденный при живой проверке I6
    [Fact]
    public async Task SameTimestamp_StillUpdatesRowVersion()
    {
        var (_, product) = await SaveProduct();
        await InDb(async db =>
        {
            var loaded = await db.Products.SingleAsync(p => p.Id == product.Id);
            loaded.ChangePrice(150m);
            await db.SaveChangesAsync();
        });
        var versionBefore = await InDb(async db =>
            db.Entry(await db.Products.SingleAsync(p => p.Id == product.Id)).Property<uint>("Version").CurrentValue);

        await InDb(async db =>
        {
            var loaded = await db.Products.Include(p => p.Images).SingleAsync(p => p.Id == product.Id);
            loaded.AddImage("a.png", "/images/a.png");   // время не двигаем
            await db.SaveChangesAsync();
        });

        var versionAfter = await InDb(async db =>
            db.Entry(await db.Products.SingleAsync(p => p.Id == product.Id)).Property<uint>("Version").CurrentValue);
        Assert.NotEqual(versionBefore, versionAfter);
    }
}
