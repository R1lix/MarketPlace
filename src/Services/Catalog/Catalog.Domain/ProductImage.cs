namespace Catalog.Domain;
public sealed class ProductImage
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string PathImage { get; private set; } = null!;
    public string NameImage { get; private set; } = null!;
    public const int MaxNameImageLength = 200;
    public const int MaxPathImageLength = 500;

    // Для EF Core
    private ProductImage() { }

    internal ProductImage(Guid productId, string nameImage, string pathImage)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
        SetPath(pathImage);
        SetName(nameImage);
    }

    private void SetPath(string pathImage)
    {
        if (string.IsNullOrWhiteSpace(pathImage))
            throw new DomainException("Путь к изображению не может быть пустым.");
        pathImage = pathImage.Trim();
        PathImage = pathImage;
    }

    private void SetName(string nameImage)
    {
        if (string.IsNullOrWhiteSpace(nameImage))
            throw new DomainException("Имя изображения не может быть пустым.");
        nameImage = nameImage.Trim();
        NameImage = nameImage;
    }
}    
