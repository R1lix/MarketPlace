using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Catalog.IntegrationTests.Infrastructure;

// Общие помощники. Тесты не очищают базу: каждый создаёт свои данные с уникальными
// артикулом и категорией, поэтому друг другу не мешают.
[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase(PostgresFixture fixture)
{
    protected PostgresFixture Fixture { get; } = fixture;

    // Выполнить действие в отдельном DbContext (как отдельный запрос к API)
    protected async Task InDb(Func<CatalogDbContext, Task> action)
    {
        await using var scope = Fixture.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());
    }

    protected async Task<T> InDb<T>(Func<CatalogDbContext, Task<T>> action)
    {
        await using var scope = Fixture.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());
    }

    // Сохраняет новую категорию и товар в ней, возвращает их
    protected async Task<(Category Category, Product Product)> SaveProduct(
        int stock = 0, int images = 0, decimal price = 100m, string? sku = null)
    {
        var category = new Category("Категория " + Guid.NewGuid().ToString("N")[..8]);
        var product = new Product("Телефон", "Описание", price, stock, category.Id, sku ?? UniqueSku());
        for (var i = 0; i < images; i++)
            product.AddImage($"img{i}.png", $"/images/{i}.png");

        await InDb(async db =>
        {
            db.Categories.Add(category);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });
        return (category, product);
    }

    protected static string UniqueSku() => "SKU-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    // Код ошибки PostgreSQL из исключения EF: 23505 — нарушение уникальности, 23503 — внешнего ключа
    protected static string? SqlState(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState;
}
