using System.Threading;
using Project.Dto.Http.Employee;

namespace Project.Integration.Tests.Factories;

public static class EmployeeObjectFabric
{
    private static int _counter;

    public static CreateEmployeeDto CreateEmployeeDto(string? fullName = null, string? email = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreateEmployeeDto(
            fullName ?? "Ivan Ivanov",
            $"+7999{idx:D7}",
            email ?? $"employee{idx}@test.local",
            new DateOnly(1990, 1, 1),
            $"photo{idx}.jpg",
            "{\"Developer\":true}");
    }

    public static UpdateEmployeeDto UpdateEmployeeDto(
        string fullName = "Petr Petrov",
        string email = "updated.employee@test.local")
    {
        return new UpdateEmployeeDto(
            fullName,
            "+79880001122",
            email,
            new DateOnly(1991, 2, 2),
            "updated.jpg",
            "{\"Lead\":true}");
    }
}

