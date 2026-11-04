namespace Category;
using Catalog.Domain;

public class Category
{
    public Guid Id { get; private set;}
    public string? Name { get; private set; }

    // Для EF Core
    public Category() {}

    public Category(string name) 
    {
        Id = Guid.CreateVersion7();
        SetName(name);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Имя категории не может быть пустым");
        Name = name;
    }
            
}