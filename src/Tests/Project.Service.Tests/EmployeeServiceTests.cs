using Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.EmployeeService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class EmployeeServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var employeeRepository = new EmployeeRepository(
            _fixture.Context,
            NullLogger<EmployeeRepository>.Instance);

        _employeeService = new EmployeeService(
            employeeRepository,
            NullLogger<EmployeeService>.Instance);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddEmployeeAsync =====

    [Fact]
    public async Task AddEmployeeAsync_ValidData_ReturnsCreatedEmployee()
    {
        // Arrange
        var creation = EmployeeObjectFabric.CreateValidCreationEmployee();

        // Act
        var result = await _employeeService.AddEmployeeAsync(
            creation.FullName, creation.PhoneNumber, creation.Email,
            creation.BirthDate, creation.Photo, creation.Duties);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.EmployeeId);
        Assert.Equal(creation.FullName, result.FullName);
        Assert.Equal(creation.Email, result.Email);
    }

    [Fact]
    public async Task AddEmployeeAsync_InvalidName_ThrowsArgumentException()
    {
        // Arrange — имя содержит цифры
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _employeeService.AddEmployeeAsync(
                "John123 Doe",
                "+71234567890",
                "test@test.ru",
                new DateOnly(1990, 1, 1),
                null, null));
    }

    [Fact]
    public async Task AddEmployeeAsync_DuplicateEmail_ThrowsEmployeeAlreadyExistsException()
    {
        // Arrange
        var creation = EmployeeObjectFabric.CreateValidCreationEmployee();
        await _employeeService.AddEmployeeAsync(
            creation.FullName, creation.PhoneNumber, creation.Email,
            creation.BirthDate, creation.Photo, creation.Duties);

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeAlreadyExistsException>(() =>
            _employeeService.AddEmployeeAsync(
                "Другой Сотрудник",
                "+79005555555",
                creation.Email, // тот же email
                new DateOnly(1995, 5, 5),
                null, null));
    }

    // ===== GetEmployeeByIdAsync =====

    [Fact]
    public async Task GetEmployeeByIdAsync_Existing_ReturnsEmployee()
    {
        // Arrange
        var creation = EmployeeObjectFabric.CreateValidCreationEmployee();
        var created = await _employeeService.AddEmployeeAsync(
            creation.FullName, creation.PhoneNumber, creation.Email,
            creation.BirthDate, creation.Photo, creation.Duties);

        // Act
        var result = await _employeeService.GetEmployeeByIdAsync(created.EmployeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.EmployeeId, result.EmployeeId);
        Assert.Equal(creation.FullName, result.FullName);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_NonExistent_ThrowsEmployeeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _employeeService.GetEmployeeByIdAsync(nonExistentId));
    }

    // ===== UpdateEmployeeAsync =====

    [Fact]
    public async Task UpdateEmployeeAsync_ValidData_ReturnsUpdatedEmployee()
    {
        // Arrange
        var creation = EmployeeObjectFabric.CreateValidCreationEmployee();
        var created = await _employeeService.AddEmployeeAsync(
            creation.FullName, creation.PhoneNumber, creation.Email,
            creation.BirthDate, creation.Photo, creation.Duties);

        var newName = "Новый Сотрудник";

        // Act
        var result = await _employeeService.UpdateEmployeeAsync(
            created.EmployeeId, newName, null, null, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newName, result.FullName);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_NonExistent_ThrowsEmployeeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _employeeService.UpdateEmployeeAsync(
                nonExistentId, "Новое Имя", null, null, null, null, null));
    }

    // ===== DeleteEmployeeAsync =====

    [Fact]
    public async Task DeleteEmployeeAsync_ExistingEmployee_DeletesSuccessfully()
    {
        // Arrange
        var creation = EmployeeObjectFabric.CreateValidCreationEmployee();
        var created = await _employeeService.AddEmployeeAsync(
            creation.FullName, creation.PhoneNumber, creation.Email,
            creation.BirthDate, creation.Photo, creation.Duties);

        // Act
        await _employeeService.DeleteEmployeeAsync(created.EmployeeId);

        // Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _employeeService.GetEmployeeByIdAsync(created.EmployeeId));
    }

    [Fact]
    public async Task DeleteEmployeeAsync_NonExistent_ThrowsEmployeeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            _employeeService.DeleteEmployeeAsync(nonExistentId));
    }
}
