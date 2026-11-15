namespace Catalog.Application.Exceptions;

// Данные изменил другой запрос, пока этот их обрабатывал (сработала проверка xmin).
// Клиенту нужно заново загрузить данные и повторить действие. Тоже 409 Conflict
public sealed class ConcurrencyConflictException(Exception? innerException = null)
    : ConflictException("Данные были изменены другим запросом. Загрузите их заново и повторите действие", innerException);
