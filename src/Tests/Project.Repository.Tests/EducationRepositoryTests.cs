using Database.Context;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.Education;
using Project.Repository.Tests.Factories;
using Xunit;

namespace Project.Repository.Tests;

/// <summary>
/// Лондонские тесты для EducationRepository.
/// Используют InMemory провайдер EF Core вместо реальной БД (Testcontainers).
/// Каждый тест получает собственную изолированную in-memory базу.
/// </summary>
public class EducationRepositoryTests : IDisposable
{
    private readonly ProjectDbContext _context;
    private readonly EducationRepository _repository;

    public EducationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectDbContext(options);
        _repository = new EducationRepository(
            _context,
            NullLogger<EducationRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ===== AddEducationAsync =====

    [Fact]
    public async Task AddEducationAsync_ShouldAddEducation_WhenNoDuplicateExists()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var education = EducationDbObjectFabric.CreateValidCreateEducation(employeeId);

        // Act
        var result = await _repository.AddEducationAsync(education);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(education.Institution, result.Institution);
        Assert.Equal(education.StudyField, result.StudyField);

        // Проверяем, что запись действительно сохранена в in-memory БД
        var dbRecord = await _context.EducationDb.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal(education.Institution, dbRecord!.Institution);
    }

    [Fact]
    public async Task AddEducationAsync_ShouldThrow_WhenDuplicateExists()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var education = EducationDbObjectFabric.CreateValidCreateEducation(employeeId);
        await _repository.AddEducationAsync(education);

        // Act & Assert — повторная вставка с теми же полями
        await Assert.ThrowsAsync<EducationAlreadyExistsException>(() =>
            _repository.AddEducationAsync(education));
    }

    // ===== GetEducationByIdAsync =====

    [Fact]
    public async Task GetEducationByIdAsync_ShouldReturnEducation_WhenExists()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var education = await _repository.AddEducationAsync(
            EducationDbObjectFabric.CreateValidCreateEducation(employeeId));

        // Act
        var result = await _repository.GetEducationByIdAsync(education.Id);

        // Assert
        Assert.Equal(education.EmployeeId, result.EmployeeId);
        Assert.Equal(education.Institution, result.Institution);
        Assert.Equal(education.StudyField, result.StudyField);
        Assert.Equal(education.StartDate, result.StartDate);
    }

    [Fact]
    public async Task GetEducationByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _repository.GetEducationByIdAsync(Guid.NewGuid()));
    }

    // ===== UpdateEducationAsync =====

    [Fact]
    public async Task UpdateEducationAsync_ShouldUpdateFields_WhenValid()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var original = await _repository.AddEducationAsync(
            EducationDbObjectFabric.CreateValidCreateEducation(employeeId));

        var update = EducationDbObjectFabric.CreateValidUpdateEducation(original.Id, employeeId);

        // Act
        var result = await _repository.UpdateEducationAsync(update);

        // Assert
        Assert.Equal(update.Institution, result.Institution);
        Assert.Equal(update.StudyField, result.StudyField);
        Assert.Equal(EducationLevel.Master, result.Level);
    }

    [Fact]
    public async Task UpdateEducationAsync_ShouldThrow_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var update = new UpdateEducation(nonExistentId, Guid.NewGuid(), "Новый вуз");

        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _repository.UpdateEducationAsync(update));
    }

    // ===== GetEducationsAsync =====

    [Fact]
    public async Task GetEducationsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            await _repository.AddEducationAsync(
                EducationDbObjectFabric.CreateValidCreateEducation(employeeId));
        }

        // Act
        var page1 = await _repository.GetEducationsAsync(employeeId, 1, 3);
        var page2 = await _repository.GetEducationsAsync(employeeId, 2, 3);

        // Assert
        var page1List = page1.ToList();
        var page2List = page2.ToList();
        Assert.Equal(3, page1List.Count);
        Assert.Equal(3, page2List.Count);
    }

    [Fact]
    public async Task GetEducationsAsync_ShouldReturnEmpty_WhenNoData()
    {
        // Act
        var result = await _repository.GetEducationsAsync(Guid.NewGuid(), 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ===== DeleteEducationAsync =====

    [Fact]
    public async Task DeleteEducationAsync_ShouldRemoveRecord_WhenExists()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var education = await _repository.AddEducationAsync(
            EducationDbObjectFabric.CreateValidCreateEducation(employeeId));

        // Act
        await _repository.DeleteEducationAsync(education.Id);

        // Assert — запись удалена, повторный запрос бросает исключение
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _repository.GetEducationByIdAsync(education.Id));
    }

    [Fact]
    public async Task DeleteEducationAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _repository.DeleteEducationAsync(Guid.NewGuid()));
    }
}
