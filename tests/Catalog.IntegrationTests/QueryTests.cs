using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

// Чтение для отображения. База общая для всех тестов, поэтому каждый тест работает
// только со своей категорией и фильтрует список по ней
public class QueryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<T> Query<T>(Func<IProductQueries, ICategoryQueries, Task<T>> action)
    {
        await using var scope = Fixture.CreateScope();
        return await action(
            scope.ServiceProvider.GetRequiredService<IProductQueries>(),
            scope.ServiceProvider.GetRequiredService<ICategoryQueries>());
    }

    // Артикул с нужным для поиска началом и уникальным хвостом: база общая, а артикул должен быть уникален
    // (хвост — шестнадцатеричные цифры, в нём нет ни «%», ни «_»)
    private static string Sku(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    // Категория с товарами с заданными названиями и артикулами
    private async Task<Category> SaveCategoryWith(params (string Name, string Sku)[] products)
    {
        var category = new Category("Запросы " + Guid.NewGuid().ToString("N")[..8]);
        await InDb(async db =>
        {
            db.Categories.Add(category);
            foreach (var (name, sku) in products)
                db.Products.Add(new Product(name, null, 10m, 1, category.Id, sku));
            await db.SaveChangesAsync();
        });
        return category;
    }

    [Fact]
    public async Task GetById_ReturnsFullCardWithCategoryAndImages()
    {
        var (category, product) = await SaveProduct(images: 2, price: 99.90m);

        var dto = await Query((products, _) => products.GetByIdAsync(product.Id));

        Assert.NotNull(dto);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(99.90m, dto.Price);
        Assert.Equal(category.Name, dto.CategoryName);
        Assert.Equal(["img0.png", "img1.png"], dto.Images.Select(i => i.Name));
    }

    // Регрессия: раньше картинки сортировались по Id, а Guid v7, созданные в одну миллисекунду,
    // идут в случайном порядке — тест падал примерно в половине запусков (так он и упал в CI).
    // С 10 картинками случайное совпадение порядка практически невозможно
    [Fact]
    public async Task GetById_ImagesAreInOrderOfAdding()
    {
        var (_, product) = await SaveProduct(images: Product.MaxImages);

        var dto = await Query((products, _) => products.GetByIdAsync(product.Id));

        var expected = Enumerable.Range(0, Product.MaxImages).Select(i => $"img{i}.png");
        Assert.Equal(expected, dto!.Images.Select(i => i.Name));
    }

    [Fact]
    public async Task GetById_DeletedOrUnknown_ReturnsNull()
    {
        var (_, product) = await SaveProduct();
        await InDb(async db =>
        {
            (await db.Products.SingleAsync(p => p.Id == product.Id)).Delete();
            await db.SaveChangesAsync();
        });

        Assert.Null(await Query((products, _) => products.GetByIdAsync(product.Id)));
        Assert.Null(await Query((products, _) => products.GetByIdAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task GetPage_FiltersByCategory_SortsByName_AndCountsTotal()
    {
        var category = await SaveCategoryWith(("Яблоко", UniqueSku()), ("Арбуз", UniqueSku()), ("Банан", UniqueSku()));
        await SaveProduct(); // товар в другой категории не должен попасть в список

        var page = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(CategoryId: category.Id)));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["Арбуз", "Банан", "Яблоко"], page.Items.Select(i => i.Name));
        Assert.All(page.Items, i => Assert.Equal(category.Name, i.CategoryName));
    }

    [Fact]
    public async Task GetPage_SplitsIntoPages()
    {
        var category = await SaveCategoryWith(
            ("A", UniqueSku()), ("B", UniqueSku()), ("C", UniqueSku()), ("D", UniqueSku()), ("E", UniqueSku()));

        var second = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(Page: 2, PageSize: 2, CategoryId: category.Id)));
        var last = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(Page: 3, PageSize: 2, CategoryId: category.Id)));

        Assert.Equal(["C", "D"], second.Items.Select(i => i.Name));
        Assert.Equal(5, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
        Assert.True(second.HasNextPage);
        Assert.Equal(["E"], last.Items.Select(i => i.Name));
        Assert.False(last.HasNextPage);
    }

    [Theory]
    [InlineData("телефон")]   // без учёта регистра
    [InlineData("ТЕЛЕ")]      // часть слова
    [InlineData("phone-77")]  // по артикулу (хранится в верхнем регистре)
    public async Task GetPage_SearchesByNameOrSku_IgnoringCase(string search)
    {
        var category = await SaveCategoryWith(("Телефон", Sku("PHONE-77")), ("Ноутбук", Sku("NOTE-1")));

        var page = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(CategoryId: category.Id, Search: search)));

        Assert.Equal(["Телефон"], page.Items.Select(i => i.Name));
    }

    // % и _ в поиске — обычные символы, а не шаблоны SQL: иначе «%» нашёл бы все товары
    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    public async Task GetPage_LikeWildcards_AreSearchedLiterally(string search)
    {
        var category = await SaveCategoryWith(("Скидка 50%", Sku("SALE_50")), ("Обычный товар", Sku("PLAIN-1")));

        var page = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(CategoryId: category.Id, Search: search)));

        Assert.Equal(["Скидка 50%"], page.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetPage_ExcludesDeletedProducts()
    {
        var (category, product) = await SaveProduct();
        await InDb(async db =>
        {
            (await db.Products.SingleAsync(p => p.Id == product.Id)).Delete();
            await db.SaveChangesAsync();
        });

        var page = await Query((products, _) => products.GetPageAsync(new ProductListQuery(CategoryId: category.Id)));

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]      // страница меньше 1 → 1
    [InlineData(1, 0, 1, 1)]        // размер меньше 1 → 1
    [InlineData(1, 1000, 1, 100)]   // размер больше максимума → 100
    public async Task GetPage_NormalizesInvalidPaging(int page, int pageSize, int expectedPage, int expectedSize)
    {
        var category = await SaveCategoryWith(("A", UniqueSku()));

        var result = await Query((products, _) =>
            products.GetPageAsync(new ProductListQuery(page, pageSize, category.Id)));

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedSize, result.PageSize);
    }

    [Fact]
    public async Task Categories_GetByIdAndAll()
    {
        var (category, _) = await SaveProduct();

        var byId = await Query((_, categories) => categories.GetByIdAsync(category.Id));
        var all = await Query((_, categories) => categories.GetAllAsync());

        Assert.Equal(category.Name, byId?.Name);
        Assert.Contains(all, c => c.Id == category.Id);
        Assert.Equal(all.Select(c => c.Name).Order(StringComparer.Ordinal), all.Select(c => c.Name), StringComparer.Ordinal);
    }

    // Чтение не должно ничего добавлять в отслеживание изменений
    [Fact]
    public async Task Queries_DoNotTrackEntities()
    {
        var (category, product) = await SaveProduct(images: 1);
        await using var scope = Fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var queries = scope.ServiceProvider.GetRequiredService<IProductQueries>();

        await queries.GetByIdAsync(product.Id);
        await queries.GetPageAsync(new ProductListQuery(CategoryId: category.Id));

        Assert.Empty(db.ChangeTracker.Entries());
    }
}
