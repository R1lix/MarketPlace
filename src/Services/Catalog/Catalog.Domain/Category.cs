namespace Catalog.Domain;

// Категория — отдельный агрегат. Product ссылается на неё только по CategoryId.
public sealed class Category
{
    public const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    // Для EF Core
    private Category() { }

    public Category(string name)
    {
        Id = Guid.CreateVersion7();
        SetName(name);
    }

    public void Rename(string name)
    {
        SetName(name);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название категории не может быть пустым");
        name = name.Trim();
        if (name.Length > MaxNameLength)
            throw new DomainException($"Название категории не может быть длиннее {MaxNameLength} символов");
        Name = name;
    }
}
