using Catalog.Application.Exceptions;

namespace Catalog.UnitTests;

public class ApplicationExceptionsTests
{
    [Fact]
    public void NotFoundException_KeepsEntityAndId()
    {
        var id = Guid.NewGuid();

        var exception = new NotFoundException("Товар", id);

        Assert.Equal("Товар", exception.EntityName);
        Assert.Equal(id, exception.Id);
        Assert.Contains(id.ToString(), exception.Message);
    }

    [Fact]
    public void ConcurrencyConflict_IsConflict()
    {
        var exception = new ConcurrencyConflictException();

        Assert.IsAssignableFrom<ConflictException>(exception);
        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }
}
