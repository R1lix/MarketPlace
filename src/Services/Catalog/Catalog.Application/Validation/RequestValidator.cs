using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ValidationException = Catalog.Application.Exceptions.ValidationException;

namespace Catalog.Application.Validation;

// Находит в DI валидатор FluentValidation для типа запроса и переводит его ошибки в ValidationException
// приложения. Так сервисам нужна одна зависимость вместо отдельного IValidator<T> на каждый запрос,
// а API не зависит от FluentValidation
internal sealed class RequestValidator(IServiceProvider serviceProvider) : IRequestValidator
{
    public async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>()
            ?? throw new InvalidOperationException($"Не зарегистрирован валидатор для {typeof(TRequest).Name}");

        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
            return;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        throw new ValidationException(errors);
    }
}
