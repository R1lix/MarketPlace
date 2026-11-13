namespace Catalog.Domain;

// Товар — корень агрегата. Картинки (ProductImage) создаются и удаляются только через него.
// UpdatedAt заполняет инфраструктура при сохранении (UpdatedAtInterceptor), домен его не трогает.
public sealed class Product : IAuditableEntity
{
    // Константы для продукта
    public const int MaxNameLength = 200;
    public const int MaxSkuLength = 50;
    public const int MaxDescriptionLength = 2000;
    public const int MaxImages = 10;
    public const int PriceDecimalPlaces = 2; // копейки; в БД колонка numeric(18, 2)

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Sku { get; private set; } = null!; // Артикул товара
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private readonly List<ProductImage> _images = new();

    // AsReadOnly: наружу отдаём обёртку, а не сам список — иначе его можно привести к List и изменить в обход AddImage
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

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

    // Обновление имени товара и его описания
    public void ChangeDetails(string name, string? description)
    {
        EnsureNotDeleted();
        SetName(name);
        SetDescription(description);
    }

    public void ChangePrice(decimal price)
    {
        EnsureNotDeleted();
        SetPrice(price);
    }

    public void ChangeCategory(Guid categoryId)
    {
        EnsureNotDeleted();
        SetCategory(categoryId);
    }

    // Приход товара на склад
    public void AddStock(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Количество для добавления должно быть больше нуля");
        SetStock(Stock + quantity);
    }

    // Списание товара со склада
    public void RemoveStock(int quantity)
    {
        EnsureNotDeleted();
        if (quantity <= 0)
            throw new DomainException("Количество для списания должно быть больше нуля");
        if (quantity > Stock)
            throw new DomainException($"Недостаточно товара: в наличии {Stock}, запрошено {quantity}");
        SetStock(Stock - quantity);
    }

    // Возвращает созданную картинку, чтобы API мог отдать её Id клиенту
    public ProductImage AddImage(string nameImage, string pathImage)
    {
        EnsureNotDeleted();
        if (_images.Count >= MaxImages)
            throw new DomainException($"У товара не может быть больше {MaxImages} картинок");
        var image = new ProductImage(Id, nameImage: nameImage, pathImage: pathImage);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(Guid imageId)
    {
        EnsureNotDeleted();
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new DomainException("Картинка не найдена");
        _images.Remove(image);
    }

    // Мягкое удаление: повторный вызов ничего не делает, товар с остатком удалить нельзя
    public void Delete()
    {
        if (IsDeleted)
            return;
        if (Stock > 0)
            throw new DomainException($"Нельзя удалить товар, на складе ещё есть остаток {Stock} единиц");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
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

    private void SetDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }
        description = description.Trim();
        if (description.Length > MaxDescriptionLength)
            throw new DomainException($"Описание товара не должно превышать {MaxDescriptionLength} символов");
        Description = description;
    }

    // TODO (улучшение): ограничить максимальную цену товара
    private void SetPrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("Цена товара не может быть отрицательной");
        // Без этой проверки 99.999 молча округлилось бы в базе до 100.00
        if (decimal.Round(price, PriceDecimalPlaces) != price)
            throw new DomainException($"У цены не может быть больше {PriceDecimalPlaces} знаков после запятой");
        Price = price;
    }

    // TODO (улучшение): защита от переполнения int в AddStock
    private void SetStock(int stock)
    {
        if (stock < 0)
            throw new DomainException("Количество товара не может быть меньше нуля");
        Stock = stock;
    }

    private void SetCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new DomainException("Id категории не может быть пустым");
        CategoryId = categoryId;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Артикул товара не может быть пустым");
        sku = sku.Trim().ToUpperInvariant();
        if (sku.Length > MaxSkuLength)
            throw new DomainException($"Длина артикула не может быть больше {MaxSkuLength} символов");
        Sku = sku;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Товар удалён, изменять его нельзя");
    }
}
