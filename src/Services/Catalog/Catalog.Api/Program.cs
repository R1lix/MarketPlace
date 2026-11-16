using Catalog.Api.ErrorHandling;
using Catalog.Application;
using Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
{
    // Иначе MVC сам объявит обязательными все string-свойства запросов и ответит на пустое поле
    // своим английским сообщением раньше наших валидаторов. Формат проверяет только FluentValidation
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Ошибки → ProblemDetails (RFC 9457): исключения приложения — через наш обработчик,
// пустые ответы с кодом ошибки (например, неизвестный адрес → 404) — через UseStatusCodePages
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Описание API: /openapi/v1.json, интерактивная страница: /swagger
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Catalog API v1");
        options.DocumentTitle = "Catalog API";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 200 Healthy — приложение запущено и база доступна, 503 Unhealthy — база недоступна
app.MapHealthChecks("/health");

app.Run();

// Нужно тестам API: WebApplicationFactory<Program> запускает приложение целиком в памяти
public partial class Program;
