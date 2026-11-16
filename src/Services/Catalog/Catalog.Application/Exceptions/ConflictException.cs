namespace Catalog.Application.Exceptions;

// Запрос корректен, но противоречит текущему состоянию данных: артикул занят,
// категория используется товарами и т. п. На шаге Api станет ответом 409 Conflict
public class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
