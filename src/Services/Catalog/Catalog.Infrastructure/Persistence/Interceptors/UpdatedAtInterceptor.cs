using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Catalog.Infrastructure.Persistence.Interceptors;

// Перед каждым SaveChanges проставляет UpdatedAt всем изменённым сущностям.
// Особый случай — агрегат Product: если поменялись только его картинки, строка товара сама по себе
// не меняется. Тогда мы «трогаем» товар: он получает новый UpdatedAt, его строка обновляется,
// и вместе с ней срабатывает проверка конкуренции xmin (иначе две одновременные AddImage
// могли бы обойти лимит в 10 картинок).
public sealed class UpdatedAtInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetUpdatedAt(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetUpdatedAt(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetUpdatedAt(DbContext? context)
    {
        if (context is null)
            return;

        // Изменения через приватные поля (например, _images) должны быть найдены до проверки состояний
        context.ChangeTracker.DetectChanges();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Товары, у которых добавили, удалили или изменили картинки
        var productsWithChangedImages = context.ChangeTracker.Entries<ProductImage>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .Select(e => e.Entity.ProductId)
            .ToHashSet();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            var changed = entry.State == EntityState.Modified
                || (entry.State == EntityState.Unchanged
                    && entry.Entity is Product product
                    && productsWithChangedImages.Contains(product.Id));

            if (changed)
                MarkUpdated(entry, now);
        }
    }

    // Запись через EF: свойство UpdatedAt в домене закрыто (private set), а EF умеет писать в него напрямую
    private static void MarkUpdated(EntityEntry<IAuditableEntity> entry, DateTime now)
    {
        var updatedAt = entry.Property(e => e.UpdatedAt);
        updatedAt.CurrentValue = now;
        // Явно: даже если время совпало с прежним значением, строка обязана обновиться —
        // иначе EF пропустит UPDATE, и проверка xmin не сработает
        updatedAt.IsModified = true;
    }
}
