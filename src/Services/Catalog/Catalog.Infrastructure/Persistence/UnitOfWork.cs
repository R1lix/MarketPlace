using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Catalog.Infrastructure.Persistence;

// DbContext уже является Unit of Work: он копит изменения и сохраняет их одной транзакцией.
// Эта обёртка скрывает EF от Application и переводит ошибки базы в ошибки приложения,
// чтобы API мог ответить 409 Conflict, ничего не зная про EF и коды PostgreSQL
internal sealed class UnitOfWork(CatalogDbContext db) : IUnitOfWork
{
    // Понятные сообщения для известных ограничений базы (имена — из миграций)
    private static readonly Dictionary<string, string> ConstraintMessages = new()
    {
        ["ix_products_sku"] = "Товар с таким артикулом уже существует",
        ["fk_products_categories_category_id"] = "Категория не существует или в ней есть товары",
    };

    // Ограничения, нарушить которые можно только одновременными изменениями: внутри агрегата их
    // соблюдает домен. Например, два запроса добавили картинку и оба назначили ей одну и ту же позицию
    private static readonly HashSet<string> ConcurrencyConstraints =
    [
        "ix_product_images_product_id_position",
    ];

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Проверка xmin: строку успел изменить другой запрос
            throw new ConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation
        } postgres)
        {
            if (postgres.ConstraintName is not null && ConcurrencyConstraints.Contains(postgres.ConstraintName))
                throw new ConcurrencyConflictException(exception);

            var message =postgres.ConstraintName is not null && ConstraintMessages.TryGetValue(postgres.ConstraintName, out var known)
                ? known
                : "Изменение противоречит данным в базе";
            throw new ConflictException(message, exception);
        }
        // Остальные ошибки (база недоступна, ошибка в SQL) — не конфликт, а сбой: пусть станут 500
    }
}
