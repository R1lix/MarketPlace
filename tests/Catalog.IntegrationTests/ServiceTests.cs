using Catalog.Application.Categories;
using Catalog.Application.Exceptions;
using Catalog.Application.Products;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

// Сквозная проверка: сервисы из AddApplication + настоящая база.
// Логика сервисов покрыта unit-тестами; здесь — что всё правильно собрано в DI и работает вместе
public class ServiceTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<T> Products<T>(Func<IProductService, Task<T>> action)
    {
        await using var scope = Fixture.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IProductService>());
    }

    private async Task Products(Func<IProductService, Task> action)
    {
        await using var scope = Fixture.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IProductService>());
    }

    private async Task<T> Categories<T>(Func<ICategoryService, Task<T>> action)
    {
        await using var scope = Fixture.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ICategoryService>());
    }

    [Fact]
    public async Task FullProductLifecycle_ThroughServices()
    {
        var category = await Categories(s => s.CreateAsync(new CreateCategoryRequest("Сервисы " + Guid.NewGuid().ToString("N")[..8])));
        var sku = UniqueSku();

        var created = await Products(s => s.CreateAsync(new CreateProductRequest("Телефон", null, 100m, 5, category.Id, sku)));
        await Products(s => s.UpdateAsync(created.Id, new UpdateProductRequest("Смартфон", "Описание", 150m, category.Id)));
        await Products(s => s.RemoveStockAsync(created.Id, new ChangeStockRequest(5)));
        var image = await Products(s => s.AddImageAsync(created.Id, new AddProductImageRequest("a.png", "/images/a.png")));

        var loaded = await Products(s => s.GetByIdAsync(created.Id));
        Assert.Equal("Смартфон", loaded.Name);
        Assert.Equal(150m, loaded.Price);
        Assert.Equal(0, loaded.Stock);
        Assert.Equal(image.Id, Assert.Single(loaded.Images).Id);
        Assert.NotNull(loaded.UpdatedAt); // проставил interceptor

        await Products(s => s.DeleteAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Products(s => s.GetByIdAsync(created.Id)));
    }

    [Fact]
    public async Task Create_DuplicateSku_ThrowsConflict()
    {
        var (category, product) = await SaveProduct();

        await Assert.ThrowsAsync<ConflictException>(() =>
            Products(s => s.CreateAsync(new CreateProductRequest("Дубль", null, 1m, 0, category.Id, product.Sku.ToLowerInvariant()))));
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_ThrowsConflict()
    {
        var (category, _) = await SaveProduct();

        await Assert.ThrowsAsync<ConflictException>(() => Categories(async s =>
        {
            await s.DeleteAsync(category.Id);
            return 0;
        }));
    }
}
