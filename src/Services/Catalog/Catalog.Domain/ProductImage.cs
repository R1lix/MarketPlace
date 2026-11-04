namespace ProducrImage
{
using Catalog.Domain;
public class ProductImage
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string PathImage { get; private set; } = null!;
    public string NameImage { get; private set; } = null!;

    // Для EF Core
    private ProductImage() { }

    internal ProductImage(Guid idProduct, string nameImage, string pathImage)
    {
        Id = Guid.CreateVersion7();
        ProductId = idProduct;
        SetPath(pathImage);
        SetName(nameImage);
    }

    internal void SetPath(string pathImage)
    {
        if (string.IsNullOrWhiteSpace(pathImage))
            throw new DomainException("Путь к изображению не может быть пустым.");
        PathImage = pathImage;
    }

    private void SetName(string nameImage)
    {
        if (string.IsNullOrWhiteSpace(nameImage))
            throw new DomainException("Имя изображения не может быть пустым.");
        NameImage = nameImage;
    }

    public void Rename(string name)
    {
        SetName(name);
    }
}    
}
