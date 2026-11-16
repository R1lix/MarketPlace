namespace Catalog.Application.Exceptions;

// Запрос некорректен: не заполнены поля, неверный формат, ссылка на несуществующую категорию.
// Ошибки сгруппированы по полям — на шаге Api станут ответом 400 с ValidationProblemDetails
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Запрос содержит ошибки")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }
}
