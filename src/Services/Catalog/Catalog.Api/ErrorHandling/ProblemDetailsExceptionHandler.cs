using System.Text.Json;
using Catalog.Application.Exceptions;
using Catalog.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.ErrorHandling;

// Единое место, где исключения превращаются в HTTP-ответы в формате ProblemDetails (RFC 9457).
// Контроллеры не ловят исключения сами: они только вызывают сервисы
internal sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            // Имена полей — как в JSON запроса (camelCase): клиент прислал "name", а не "Name"
            ValidationException validation => new ValidationProblemDetails(validation.Errors.ToDictionary(
                e => JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Запрос содержит ошибки",
            },
            DomainException => Problem(StatusCodes.Status400BadRequest, "Нарушено бизнес-правило", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Не найдено", exception.Message),
            ConcurrencyConflictException => Problem(StatusCodes.Status409Conflict, "Конфликт одновременных изменений", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Конфликт", exception.Message),
            // Детали неожиданной ошибки клиенту не показываем — они только в логах
            _ => Problem(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", null),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Необработанная ошибка при запросе {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Запрос {Method} {Path} отклонён: {Status} {Message}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, exception.Message);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
