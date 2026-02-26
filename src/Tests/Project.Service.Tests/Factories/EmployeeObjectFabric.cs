using Project.Core.Models.Employee;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Employee.
/// </summary>
public static class EmployeeObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный объект CreationEmployee с уникальными данными.
    /// </summary>
    public static CreationEmployee CreateValidCreationEmployee(string? fullName = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreationEmployee(
            fullName ?? $"Иван Тестов",
            $"+7977{idx:D7}",
            $"employee{idx}@test.ru",
            new DateOnly(1990, 1, 1),
            null,
            null
        );
    }

    /// <summary>
    /// Создаёт валидный объект CreationEmployee с фото и обязанностями.
    /// </summary>
    public static CreationEmployee CreateFullCreationEmployee()
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreationEmployee(
            "Пётр Сидоров",
            $"+7900{idx:D7}",
            $"full{idx}@test.ru",
            new DateOnly(1985, 5, 15),
            "photo.jpg",
            "{\"Developer\": true}"
        );
    }

    /// <summary>
    /// Создаёт валидный объект UpdateEmployee.
    /// </summary>
    public static UpdateEmployee CreateValidUpdateEmployee(Guid employeeId, string? fullName = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new UpdateEmployee(
            employeeId,
            fullName ?? "Сидоров Пётр",
            $"+7911{idx:D7}",
            $"upd{idx}@test.ru",
            new DateOnly(1992, 3, 20),
            null,
            null
        );
    }
}

