using Database.Models;
using Project.Core.Models.Employee;

namespace Project.Repository.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Employee (DB и Core модели).
/// </summary>
public static class EmployeeDbObjectFabric
{
    private static int _counter;

    private static readonly string[] LastNames =
        ["Иванов", "Петров", "Сидоров", "Козлов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Новиков", "Морозов"];

    private static readonly string[] FirstNames =
        ["Иван", "Пётр", "Алексей", "Дмитрий", "Сергей", "Андрей", "Максим", "Николай", "Михаил", "Артём"];

    private static readonly string[] Patronymics =
        ["Иванович", "Петрович", "Алексеевич", "Дмитриевич", "Сергеевич", "Андреевич", "Максимович", "Николаевич", "Михайлович", "Артёмович"];

    private static string GenerateName(int idx)
    {
        var lastIdx = idx % LastNames.Length;
        var firstIdx = idx % FirstNames.Length;
        var patronymicIdx = idx % Patronymics.Length;
        return $"{LastNames[lastIdx]} {FirstNames[firstIdx]} {Patronymics[patronymicIdx]}";
    }

    /// <summary>
    /// Создаёт валидный EmployeeDb для прямой вставки в БД.
    /// </summary>
    public static EmployeeDb CreateValidEmployeeDb(string? fullName = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new EmployeeDb(
            Guid.NewGuid(),
            fullName ?? GenerateName(idx),
            $"+7977{idx:D7}",
            $"employee{idx}@test.ru",
            new DateOnly(1990, 1, 1),
            null,
            null
        );
    }

    /// <summary>
    /// Создаёт валидный CreationEmployee для вызова репозитория.
    /// </summary>
    public static CreationEmployee CreateValidCreationEmployee(string? fullName = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreationEmployee(
            fullName ?? GenerateName(idx),
            $"+7977{idx:D7}",
            $"employee{idx}@test.ru",
            new DateOnly(1990, 1, 1),
            "photo.jpg",
            "{\"Developer\": true}"
        );
    }

    /// <summary>
    /// Создаёт валидный UpdateEmployee.
    /// </summary>
    public static UpdateEmployee CreateValidUpdateEmployee(Guid employeeId)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new UpdateEmployee(
            employeeId,
            GenerateName(idx + 50),
            $"+7911{idx:D7}",
            $"updated_emp{idx}@test.ru",
            new DateOnly(1992, 3, 20),
            "new-photo.jpg",
            "{\"Manager\": true}"
        );
    }
}
