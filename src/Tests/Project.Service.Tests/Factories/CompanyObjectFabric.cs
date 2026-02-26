using Project.Core.Models.Company;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Company.
/// </summary>
public static class CompanyObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный объект CreationCompany с уникальными данными.
    /// </summary>
    public static CreationCompany CreateValidCreationCompany(string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreationCompany(
            title ?? $"Test Company {idx}",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            $"+7977{idx:D7}",
            $"company{idx}@test.ru",
            $"{idx % 10000000000:D10}",
            $"{idx % 1000000000:D9}",
            $"{idx % 10000000000000:D13}",
            $"г. Москва, ул. Тестовая, д. {idx}"
        );
    }

    /// <summary>
    /// Создаёт валидный объект UpdateCompany.
    /// </summary>
    public static UpdateCompany CreateValidUpdateCompany(Guid companyId, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new UpdateCompany(
            companyId,
            title ?? $"Updated Company {idx}",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            $"+7900{idx:D7}",
            $"updated{idx}@test.ru",
            $"{(idx + 5000000000) % 10000000000:D10}",
            $"{(idx + 500000000) % 1000000000:D9}",
            $"{(idx + 5000000000000) % 10000000000000:D13}",
            $"г. Москва, ул. Обновлённая, д. {idx}"
        );
    }
}

