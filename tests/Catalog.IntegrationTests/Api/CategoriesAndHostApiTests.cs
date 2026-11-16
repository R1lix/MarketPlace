using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Categories;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Api;

public class CategoriesApiTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Category_CreateGetRenameListDelete()
    {
        var created = await CreateCategory("  Смартфоны " + Guid.NewGuid().ToString("N")[..6]);
        Assert.StartsWith("Смартфоны", created.Name); // пробелы обрезаны

        var renamed = await Client.PutAsJsonAsync($"/api/categories/{created.Id}", new RenameCategoryRequest("Телефоны " + created.Id));
        var loaded = await Client.GetFromJsonAsync<CategoryDto>($"/api/categories/{created.Id}", Json);
        var all = await Client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", Json);
        var deleted = await Client.DeleteAsync($"/api/categories/{created.Id}");
        var again = await Client.DeleteAsync($"/api/categories/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);
        Assert.Equal("Телефоны " + created.Id, loaded!.Name);
        Assert.Contains(all!, c => c.Id == created.Id);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await ReadProblem(again, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(" "));

        var problem = await ReadProblem(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("name", out _));
    }

    // Поле не прислали вовсе — та же понятная ошибка нашего валидатора, а не английская от MVC
    [Fact]
    public async Task Create_MissingField_Returns400FromOurValidator()
    {
        var response = await Client.PostAsJsonAsync("/api/categories", new { });

        var problem = await ReadProblem(response, HttpStatusCode.BadRequest);
        Assert.Equal("Поле обязательно", problem.GetProperty("errors").GetProperty("name")[0].GetString());
    }

    [Fact]
    public async Task Delete_WithProducts_Returns409()
    {
        var category = await CreateCategory();
        await CreateProduct(category.Id);

        var response = await Client.DeleteAsync($"/api/categories/{category.Id}");

        await ReadProblem(response, HttpStatusCode.Conflict);
    }
}

// Всё, что не относится к конкретному ресурсу: здоровье, описание API, неизвестные адреса
public class HostApiTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Health_IsHealthy()
    {
        var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApiDocument_DescribesEndpoints()
    {
        var document = await Client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/products/{id}/stock/remove", document);
        Assert.Contains("/api/categories", document);
        Assert.Contains("Списание товара со склада", document); // описания методов на русском
    }

    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        var response = await Client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_Returns404Problem()
    {
        var response = await Client.GetAsync("/api/unknown");

        await ReadProblem(response, HttpStatusCode.NotFound);
    }

    // Неожиданная ошибка (здесь — база недоступна): 500, и никаких подробностей клиенту —
    // текст исключения может раскрыть адрес базы, SQL или устройство кода. Подробности только в логах
    [Fact]
    public async Task UnexpectedError_Returns500WithoutInternalDetails()
    {
        await using var brokenApi = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                $"ConnectionStrings:{Catalog.Infrastructure.DependencyInjection.ConnectionStringName}",
                "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2"));
        using var client = brokenApi.CreateClient();

        var response = await client.GetAsync("/api/categories");

        var problem = await ReadProblem(response, HttpStatusCode.InternalServerError);
        Assert.False(problem.TryGetProperty("detail", out var detail) && detail.ValueKind == System.Text.Json.JsonValueKind.String);
        var body = problem.GetRawText();
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain("127.0.0.1", body);
    }
}
