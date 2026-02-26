using Project.Core.Models.Employee;
using Project.Dto.Http.Employee;

namespace Project.Controller.Tests.Factories;

public static class EmployeeObjectFabric
{
    public static CreateEmployeeDto CreateEmployeeDto(string fullName = "Ivan Ivanov")
    {
        return new CreateEmployeeDto(
            fullName,
            "+71234567890",
            "employee@test.com",
            new DateOnly(1990, 1, 1),
            "photo.jpg",
            "{\"Developer\":true}");
    }

    public static UpdateEmployeeDto UpdateEmployeeDto(string fullName = "Petr Petrov")
    {
        return new UpdateEmployeeDto(
            fullName,
            "+79876543210",
            "updated@test.com",
            new DateOnly(1991, 2, 2),
            "new_photo.jpg",
            "{\"Lead\":true}");
    }

    public static BaseEmployee BaseEmployee(Guid employeeId, string fullName = "Ivan Ivanov")
    {
        return new BaseEmployee(
            employeeId,
            fullName,
            "+71234567890",
            "employee@test.com",
            new DateOnly(1990, 1, 1),
            "photo.jpg",
            "{\"Developer\":true}");
    }
}

