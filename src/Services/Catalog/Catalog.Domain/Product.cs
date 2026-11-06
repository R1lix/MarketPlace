namespace Catalog.Domain;
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Sku {  get; private set; } = null!; // Артикул товара
    public DateTime CreatedAt { get; private set;}
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    private readonly List<ProductImage> _images = new List<ProductImage>();
    public IReadOnlyCollection<ProductImage> Images => _images;

    public const int MaxNameLength = 200; 
    public const int MaxSkuLength = 50;
    public const int MaxDescriptionLength = 2000;

    // Для EF Core
    private Product() { }

    // Стандартный конструктор
    public Product(string name, string? description, decimal price, int stock, Guid categoryId, string sku)
    {
        Id = Guid.CreateVersion7();
        SetName(name);
        SetDescription(description);
        SetPrice(price);
        SetStock(stock);
        SetCategory(categoryId);
        SetSku(sku);
        CreatedAt = DateTime.UtcNow;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Артикул товара не может быть пустым");
        sku = sku.Trim();
        sku = sku.ToUpperInvariant();
        if (sku.Length > MaxSkuLength)
            throw new DomainException($"Длина артикула не может быть больше {MaxSkuLength} символов");
        Sku = sku;
    }

    private void SetDescription(string? description)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            description = description.Trim();
            if (description.Length > MaxDescriptionLength)
                throw new DomainException($"Описание товара не должно превышать {MaxDescriptionLength} символов");         
            Description = description;
        }
        else
        {
            Description = null;
        }
        
    }
    private void SetCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new DomainException("Id категории не может быть пустым");
        CategoryId = categoryId;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название товара не может быть пустым");
        name = name.Trim();
        if (name.Length > MaxNameLength)
            throw new DomainException($"Длина названия не может быть больше {MaxNameLength} символов");      
        Name = name;
    }

    // ОГРАНИЧИТЬ МАКСИМАЛЬНО ВОЗМОЖНУЮ ЦЕНУ ТОВАРА
    private void SetPrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("Цена товара не может быть отрицательной");
        Price = price;
    }

    // ОГРАНИЧИТЬ МАКСИМАЛЬНО ВОЗМОЖНОЕ КОЛИЧЕСТВО ТОВАРА
    private void SetStock(int stock)
    {
        if (stock < 0)
            throw new DomainException("Количество товара не может быть меньше нуля");
        Stock = stock;
    }

    // Обновление имени товара и его описания
    public void ChangeDetails(string name, string? description)
    {
        EnsureNotDeleted();
        SetName(name);
        SetDescription(description);
        MarkUpdated();
    }

    public void ChangePrice(decimal price)
    {
        EnsureNotDeleted();
        SetPrice(price);
        MarkUpdated();
    }

    // Время обновления вынесено в отдельный метод
    // Перенесети потом в Infrastructure
    private void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
            return;
        IsDeleted = true;
        MarkDeletedUpdateAt();
    }

    private void MarkDeletedUpdateAt()
    {
        DeletedAt = DateTime.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Товар удален");
    }
}
