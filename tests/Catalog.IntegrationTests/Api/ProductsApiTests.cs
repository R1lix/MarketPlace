using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Api;

public class ProductsApiTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Create_Returns201WithLocationAndBody()
    {
        var category = await CreateCategory();

        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("  Телефон  ", null, 99.90m, 3, category.Id, " ab-" + Guid.NewGuid().ToString("N")[..6]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ProductDto>(Json))!;
        Assert.Equal($"/api/products/{created.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Телефон", created.Name);
        Assert.StartsWith("AB-", created.Sku);
        Assert.Equal(category.Name, created.CategoryName);

        var loaded = await Client.GetFromJsonAsync<ProductDto>(response.Headers.Location, Json);
        Assert.Equal(created.Id, loaded!.Id);
    }

    [Fact]
    public async Task Create_InvalidFormat_Returns400WithErrorsByJsonFieldNames()
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("", null, 1.001m, -1, Guid.Empty, ""));

        var problem = await ReadProblem(response, HttpStatusCode.BadRequest);
        var fields = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order();
        Assert.Equal(["categoryId", "name", "price", "sku", "stock"], fields);
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns400ForCategoryField()
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Телефон", null, 1m, 0, Guid.NewGuid(), UniqueSku()));

        var problem = await ReadProblem(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("categoryId", out _));
    }

    [Fact]
    public async Task Create_DuplicateSku_Returns409()
    {
        var category = await CreateCategory();
        var existing = await CreateProduct(category.Id);

        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Дубль", null, 1m, 0, category.Id, existing.Sku.ToLowerInvariant()));

        var problem = await ReadProblem(response, HttpStatusCode.Conflict);
        Assert.Contains(existing.Sku, problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400()
    {
        var response = await Client.PostAsync("/api/products",
            new StringContent("{ \"name\": ", Encoding.UTF8, "application/json"));

        await ReadProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404Problem()
    {
        var id = Guid.NewGuid();

        var response = await Client.GetAsync($"/api/products/{id}");

        var problem = await ReadProblem(response, HttpStatusCode.NotFound);
        Assert.Contains(id.ToString(), problem.GetProperty("detail").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _)); // по нему ошибку можно найти в логах
    }

    [Fact]
    public async Task List_DefaultPaging_AndFilterAndSearch()
    {
        var category = await CreateCategory();
        await CreateProduct(category.Id);
        var other = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Ноутбук", null, 1m, 0, category.Id, UniqueSku()));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);

        var all = await Client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?categoryId={category.Id}", Json);
        var found = await Client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?categoryId={category.Id}&search=ноут", Json);

        // Без параметров страницы — значения по умолчанию из ProductListQuery
        Assert.Equal(1, all!.Page);
        Assert.Equal(ProductListQuery.DefaultPageSize, all.PageSize);
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(["Ноутбук"], found!.Items.Select(i => i.Name));
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task List_InvalidPaging_Returns400(string query)
    {
        var response = await Client.GetAsync($"/api/products?{query}");

        await ReadProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_NotANumber_Returns400()
    {
        var response = await Client.GetAsync("/api/products?page=abc");

        await ReadProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns204AndChangesCard()
    {
        var category = await CreateCategory();
        var product = await CreateProduct(category.Id);

        var response = await Client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductRequest("Смартфон", "Новое описание", 250m, category.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var loaded = await Client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.Equal("Смартфон", loaded!.Name);
        Assert.Equal(250m, loaded.Price);
        Assert.NotNull(loaded.UpdatedAt);
    }

    [Fact]
    public async Task Stock_AddAndRemove_AndBusinessRuleViolationIs400()
    {
        var category = await CreateCategory();
        var product = await CreateProduct(category.Id, stock: 1);

        var add = await Client.PostAsJsonAsync($"/api/products/{product.Id}/stock/add", new ChangeStockRequest(4));
        var remove = await Client.PostAsJsonAsync($"/api/products/{product.Id}/stock/remove", new ChangeStockRequest(2));
        var tooMuch = await Client.PostAsJsonAsync($"/api/products/{product.Id}/stock/remove", new ChangeStockRequest(100));

        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
        var problem = await ReadProblem(tooMuch, HttpStatusCode.BadRequest);
        Assert.Contains("Недостаточно товара", problem.GetProperty("detail").GetString());
        var loaded = await Client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.Equal(3, loaded!.Stock);
    }

    [Fact]
    public async Task Images_AddAndRemove()
    {
        var category = await CreateCategory();
        var product = await CreateProduct(category.Id);

        var added = await Client.PostAsJsonAsync($"/api/products/{product.Id}/images",
            new AddProductImageRequest("photo.png", "/images/photo.png"));
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal($"/api/products/{product.Id}", added.Headers.Location?.AbsolutePath);
        var image = (await added.Content.ReadFromJsonAsync<ProductImageDto>(Json))!;

        var unknown = await Client.DeleteAsync($"/api/products/{product.Id}/images/{Guid.NewGuid()}");
        var removed = await Client.DeleteAsync($"/api/products/{product.Id}/images/{image.Id}");

        await ReadProblem(unknown, HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        var loaded = await Client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.Empty(loaded!.Images);
    }

    [Fact]
    public async Task Delete_WithStockIs400_ThenDeleteAnd404()
    {
        var category = await CreateCategory();
        var product = await CreateProduct(category.Id, stock: 2);

        var withStock = await Client.DeleteAsync($"/api/products/{product.Id}");
        await ReadProblem(withStock, HttpStatusCode.BadRequest);

        await Client.PostAsJsonAsync($"/api/products/{product.Id}/stock/remove", new ChangeStockRequest(2));
        var deleted = await Client.DeleteAsync($"/api/products/{product.Id}");
        var again = await Client.DeleteAsync($"/api/products/{product.Id}");
        var get = await Client.GetAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await ReadProblem(again, HttpStatusCode.NotFound);
        await ReadProblem(get, HttpStatusCode.NotFound);
    }

    // Строки в JSON-ответе: числа — числами, Guid — строками, имена полей — camelCase
    [Fact]
    public async Task Response_UsesCamelCaseJson()
    {
        var category = await CreateCategory();
        var product = await CreateProduct(category.Id);

        var json = await Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}", Json);

        Assert.Equal(199.90m, json.GetProperty("price").GetDecimal());
        Assert.Equal(category.Name, json.GetProperty("categoryName").GetString());
        Assert.Equal(JsonValueKind.Array, json.GetProperty("images").ValueKind);
    }
}
