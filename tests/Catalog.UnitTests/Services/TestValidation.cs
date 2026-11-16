using Catalog.Application;
using Catalog.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.UnitTests.Services;

// Настоящий валидатор с настоящими правилами — так же, как в приложении (AddApplication).
// Заглушка тут не нужна: валидаторы не ходят ни в базу, ни в сеть
internal static class TestValidation
{
    private static readonly IServiceProvider Services = new ServiceCollection().AddApplication().BuildServiceProvider();

    public static IRequestValidator Validator => Services.GetRequiredService<IRequestValidator>();
}
