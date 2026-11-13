namespace Catalog.Domain;

// Сущность с датами создания и изменения.
// CreatedAt задаёт сам домен в конструкторе, UpdatedAt — инфраструктура при сохранении изменений.
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
