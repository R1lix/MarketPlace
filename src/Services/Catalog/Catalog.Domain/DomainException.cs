namespace Catalog.Domain;

// Нарушение бизнес-правила. На шаге Api станет ответом 400 Bad Request,
// а любое другое исключение — ответом 500 (ошибка в коде).
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
