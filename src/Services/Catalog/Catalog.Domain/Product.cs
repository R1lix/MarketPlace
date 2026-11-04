namespace Catalog.Domain;
sealed public class Product
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
    public bool IsDeleted { get; private set; }
    private readonly List<ProductImage> _images = new List<ProductImage>();
    public IReadOnlyCollection<ProductImage> Images => _images;

    private const int MAX_NAME_LENGHT = 200; 
    private const int MAX_SKU_LENGHT = 50;
    private const int MAX_DESCRIPTION_LENGHT = 2000;

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
        if (sku.Length > MAX_SKU_LENGHT)
            throw new DomainException($"Длина артикула не может быть больше {MAX_SKU_LENGHT} символов");
        sku = sku.ToUpperInvariant();
        Sku = sku;
    }

    private void SetDescription(string? description)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            if (description.Length > MAX_DESCRIPTION_LENGHT)
                throw new DomainException($"Описание товара не должно превышать {MAX_DESCRIPTION_LENGHT} символов");
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
        if (name.Length > MAX_NAME_LENGHT)
            throw new DomainException($"Длина названия не может быть больше {MAX_NAME_LENGHT} символов");
        name = name.Trim();
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
        SetName(name);
        SetDescription(description);
        MarkUpdated();
    }

    public void ChangePrice(decimal price)
    {
        SetPrice(price);
        MarkUpdated();
    }

    // Время обновления вынесено в отдельный метод
    // Перенесети потом в Infrastructure
    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

}
