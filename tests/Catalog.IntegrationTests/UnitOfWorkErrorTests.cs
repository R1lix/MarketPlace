using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

// UnitOfWork переводит ошибки EF и PostgreSQL в ошибки приложения
public class UnitOfWorkErrorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private sealed record Services(IProductRepository Products, ICategoryRepository Categories, IUnitOfWork UnitOfWork);

    private static Services Resolve(AsyncServiceScope scope) => new(
        scope.ServiceProvider.GetRequiredService<IProductRepository>(),
        scope.ServiceProvider.GetRequiredService<ICategoryRepository>(),
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>());

    [Fact]
    public async Task DuplicateSku_BecomesConflictWithClearMessage()
    {
        var (category, product) = await SaveProduct();
        await using var scope = Fixture.CreateScope();
        var s = Resolve(scope);

        s.Products.Add(new Product("Дубль", null, 1m, 0, category.Id, product.Sku));
        var exception = await Assert.ThrowsAsync<ConflictException>(() => s.UnitOfWork.SaveChangesAsync());

        Assert.Equal("Товар с таким артикулом уже существует", exception.Message);
        Assert.IsType<DbUpdateException>(exception.InnerException); // исходная ошибка сохранена для логов
    }

    [Fact]
    public async Task ProductWithUnknownCategory_BecomesConflict()
    {
        await using var scope = Fixture.CreateScope();
        var s = Resolve(scope);

        s.Products.Add(new Product("Телефон", null, 1m, 0, Guid.NewGuid(), UniqueSku()));

        await Assert.ThrowsAsync<ConflictException>(() => s.UnitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteCategoryWithProducts_BecomesConflict()
    {
        var (category, _) = await SaveProduct();
        await using var scope = Fixture.CreateScope();
        var s = Resolve(scope);

        s.Categories.Remove((await s.Categories.GetByIdAsync(category.Id))!);

        await Assert.ThrowsAsync<ConflictException>(() => s.UnitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentChange_BecomesConcurrencyConflict()
    {
        var (_, product) = await SaveProduct();
        await using var scope1 = Fixture.CreateScope();
        await using var scope2 = Fixture.CreateScope();
        var first = Resolve(scope1);
        var second = Resolve(scope2);
        var p1 = await first.Products.GetByIdAsync(product.Id);
        var p2 = await second.Products.GetByIdAsync(product.Id);

        p1!.ChangePrice(200m);
        await first.UnitOfWork.SaveChangesAsync();
        p2!.ChangePrice(300m);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.UnitOfWork.SaveChangesAsync());
        Assert.IsAssignableFrom<ConflictException>(exception); // для API это тоже 409
    }

    // Гонка двух AddImage: какой бы защитой её ни поймала база (xmin или индекс позиций),
    // клиент получает одну и ту же понятную ошибку «данные изменены другим запросом»
    [Fact]
    public async Task ConcurrentAddImage_BecomesConcurrencyConflict()
    {
        var (_, product) = await SaveProduct(images: 2);
        await using var scope1 = Fixture.CreateScope();
        await using var scope2 = Fixture.CreateScope();
        var first = Resolve(scope1);
        var second = Resolve(scope2);
        var p1 = await first.Products.GetByIdAsync(product.Id);
        var p2 = await second.Products.GetByIdAsync(product.Id);

        p1!.AddImage("a.png", "/images/a.png");
        await first.UnitOfWork.SaveChangesAsync();
        p2!.AddImage("b.png", "/images/b.png");

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.UnitOfWork.SaveChangesAsync());
    }
}
