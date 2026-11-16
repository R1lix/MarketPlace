using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Application.Categories;
using Catalog.Application.Products;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Api;

// Тесты HTTP-уровня: запрос идёт через весь конвейер ASP.NET Core — маршруты, привязку JSON,
// контроллер, сервис, базу и обработчик ошибок. Проверяем то, что видит клиент: коды, заголовки, JSON
public abstract class ApiTestBase(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    protected static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    protected HttpClient Client { get; } = fixture.CreateApiClient();

    protected async Task<CategoryDto> CreateCategory(string? name = null)
    {
        var response = await Client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest(name ?? "API " + Guid.NewGuid().ToString("N")[..8]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(Json))!;
    }

    protected async Task<ProductDto> CreateProduct(Guid categoryId, int stock = 0, string? sku = null)
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Телефон", "Описание", 199.90m, stock, categoryId, sku ?? UniqueSku()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>(Json))!;
    }

    // Ответ об ошибке в формате ProblemDetails (RFC 9457)
    protected static async Task<JsonElement> ReadProblem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("title", out _));
        return problem;
    }
}
