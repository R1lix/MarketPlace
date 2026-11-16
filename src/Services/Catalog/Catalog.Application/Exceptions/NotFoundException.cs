namespace Catalog.Application.Exceptions;

// Запрошенных данных нет (или товар удалён). На шаге Api станет ответом 404 Not Found
public class NotFoundException(string entityName, Guid id)
    : Exception($"{entityName} с Id {id} не найден(а)")
{
    public string EntityName { get; } = entityName;
    public Guid Id { get; } = id;
}
