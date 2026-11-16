namespace Catalog.Application.Validation;

// Проверка формата запроса до обращения к базе и домену.
// Нарушения собираются все сразу (а не до первой ошибки) и выбрасываются одним ValidationException → 400
public interface IRequestValidator
{
    Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
}
