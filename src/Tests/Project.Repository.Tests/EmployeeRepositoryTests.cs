using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class EmployeeRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly EmployeeRepository _repository;

    public EmployeeRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new EmployeeRepository(
            _fixture.Context,
            NullLogger<EmployeeRepository>.Instance);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddEmployeeAsync =====

    [Fact]
    public async Task AddEmployee_Successful()
    {
        // Arrange
        var employeeToAdd = EmployeeDbObjectFabric.CreateValidCreationEmployee("Иванов Иван");

        // Act
        var result = await _repository.AddEmployeeAsync(employeeToAdd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeToAdd.FullName, result.FullName);
        Assert.Equal(employeeToAdd.Email, result.Email);
        Assert.Equal(employeeToAdd.PhoneNumber, result.PhoneNumber);
        Assert.Equal(employeeToAdd.BirthDate, result.BirthDate);

        var savedEmployee = await _fixture.Context.EmployeeDb
            .FirstOrDefaultAsync(e => e.Id == result.EmployeeId);
        Assert.NotNull(savedEmployee);
        Assert.Equal(employeeToAdd.FullName, savedEmployee!.FullName);
    }

    [Fact]
    public async Task AddEmployee_AlreadyExists_ThrowsException()
    {
        // Arrange
        var employeeToAdd = EmployeeDbObjectFabric.CreateValidCreationEmployee();
        await _repository.AddEmployeeAsync(employeeToAdd);

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeAlreadyExistsException>(() =>
            _repository.AddEmployeeAsync(employeeToAdd));
    }

    // ===== GetEmployeeByIdAsync =====

    [Fact]
    public async Task GetEmployeeById_Successful()
    {
        // Arrange
        var employeeToAdd = EmployeeDbObjectFabric.CreateValidCreationEmployee();
        var addedEmployee = await _repository.AddEmployeeAsync(employeeToAdd);

        // Act
        var result = await _repository.GetEmployeeByIdAsync(addedEmployee.EmployeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedEmployee.EmployeeId, result.EmployeeId);
        Assert.Equal(addedEmployee.FullName, result.FullName);
        Assert.Equal(addedEmployee.Email, result.Email);
    }

    [Fact]
    public async Task GetEmployeeById_NotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _repository.GetEmployeeByIdAsync(Guid.NewGuid()));
    }

    // ===== UpdateEmployeeAsync =====

    [Fact]
    public async Task UpdateEmployee_Successful()
    {
        // Arrange
        var employeeToAdd = EmployeeDbObjectFabric.CreateValidCreationEmployee();
        var addedEmployee = await _repository.AddEmployeeAsync(employeeToAdd);

        var updateEmployee = EmployeeDbObjectFabric.CreateValidUpdateEmployee(addedEmployee.EmployeeId);

        // Act
        var result = await _repository.UpdateEmployeeAsync(updateEmployee);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateEmployee.FullName, result.FullName);
        Assert.Equal(updateEmployee.Email, result.Email);
        Assert.Equal(updateEmployee.PhoneNumber, result.PhoneNumber);

        using var freshCtx = _fixture.CreateFreshContext();
        var updatedEmployee = await freshCtx.EmployeeDb
            .FirstOrDefaultAsync(e => e.Id == result.EmployeeId);
        Assert.NotNull(updatedEmployee);
        Assert.Equal(updateEmployee.FullName, updatedEmployee!.FullName);
    }

    [Fact]
    public async Task UpdateEmployee_NotFound_ThrowsException()
    {
        // Arrange
        var updateEmployee = EmployeeDbObjectFabric.CreateValidUpdateEmployee(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _repository.UpdateEmployeeAsync(updateEmployee));
    }

    // ===== DeleteEmployeeAsync =====

    [Fact]
    public async Task DeleteEmployee_Successful()
    {
        // Arrange
        var employeeToAdd = EmployeeDbObjectFabric.CreateValidCreationEmployee();
        var addedEmployee = await _repository.AddEmployeeAsync(employeeToAdd);

        // Act
        await _repository.DeleteEmployeeAsync(addedEmployee.EmployeeId);

        // Assert
        var deletedEmployee = await _fixture.Context.EmployeeDb
            .FirstOrDefaultAsync(e => e.Id == addedEmployee.EmployeeId);
        Assert.Null(deletedEmployee);
    }

    [Fact]
    public async Task DeleteEmployee_NotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _repository.DeleteEmployeeAsync(Guid.NewGuid()));
    }
}
