using Catalog.Application.Abstractions;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

// Репозитории и Unit of Work через DI — так же, как их получат сервисы приложения
public class RepositoryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task InScope(Func<IProductRepository, ICategoryRepository, IUnitOfWork, Task> action)
    {
        await using var scope = Fixture.CreateScope();
        var sp = scope.ServiceProvider;
        await action(
            sp.GetRequiredService<IProductRepository>(),
            sp.GetRequiredService<ICategoryRepository>(),
            sp.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public async Task AddAndSave_ThroughRepositories_PersistsAggregate()
    {
        var category = new Category("Категория " + Guid.NewGuid().ToString("N")[..8]);
        var product = new Product("Телефон", null, 100m, 1, category.Id, UniqueSku());
        product.AddImage("a.png", "/images/a.png");

        await InScope(async (products, categories, unitOfWork) =>
        {
            categories.Add(category);
            products.Add(product);
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (products, _, _) =>
        {
            var loaded = await products.GetByIdAsync(product.Id);
            Assert.NotNull(loaded);
            Assert.Single(loaded.Images); // агрегат загружается вместе с картинками
        });
    }

    [Fact]
    public async Task GetById_DeletedProduct_ReturnsNull()
    {
        var (_, product) = await SaveProduct();
        await InScope(async (products, _, unitOfWork) =>
        {
            var loaded = await products.GetByIdAsync(product.Id);
            loaded!.Delete();
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (products, _, _) => Assert.Null(await products.GetByIdAsync(product.Id)));
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        await InScope(async (products, categories, _) =>
        {
            Assert.Null(await products.GetByIdAsync(Guid.NewGuid()));
            Assert.Null(await categories.GetByIdAsync(Guid.NewGuid()));
        });
    }

    [Fact]
    public async Task SkuExists_IgnoresDeletedProducts()
    {
        var (_, active) = await SaveProduct();
        var (_, deleted) = await SaveProduct();
        await InScope(async (products, _, unitOfWork) =>
        {
            (await products.GetByIdAsync(deleted.Id))!.Delete();
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (products, _, _) =>
        {
            Assert.True(await products.SkuExistsAsync(active.Sku));
            Assert.False(await products.SkuExistsAsync(deleted.Sku));
            Assert.False(await products.SkuExistsAsync(UniqueSku()));
        });
    }

    [Fact]
    public async Task Category_ExistsAndHasProducts()
    {
        var (withProducts, _) = await SaveProduct();
        var empty = new Category("Пустая " + Guid.NewGuid().ToString("N")[..8]);
        await InScope(async (_, categories, unitOfWork) =>
        {
            categories.Add(empty);
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (_, categories, _) =>
        {
            Assert.True(await categories.ExistsAsync(withProducts.Id));
            Assert.False(await categories.ExistsAsync(Guid.NewGuid()));
            Assert.True(await categories.HasProductsAsync(withProducts.Id));
            Assert.False(await categories.HasProductsAsync(empty.Id));
        });
    }

    // Удалённый товар тоже держит категорию (внешний ключ), поэтому HasProducts должен его видеть
    [Fact]
    public async Task HasProducts_CountsDeletedProducts()
    {
        var (category, product) = await SaveProduct();
        await InScope(async (products, _, unitOfWork) =>
        {
            (await products.GetByIdAsync(product.Id))!.Delete();
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (_, categories, _) => Assert.True(await categories.HasProductsAsync(category.Id)));
    }

    [Fact]
    public async Task RemoveCategory_WithoutProducts_DeletesIt()
    {
        var category = new Category("Удаляемая " + Guid.NewGuid().ToString("N")[..8]);
        await InScope(async (_, categories, unitOfWork) =>
        {
            categories.Add(category);
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (_, categories, unitOfWork) =>
        {
            categories.Remove((await categories.GetByIdAsync(category.Id))!);
            await unitOfWork.SaveChangesAsync();
        });

        await InScope(async (_, categories, _) => Assert.False(await categories.ExistsAsync(category.Id)));
    }
}
