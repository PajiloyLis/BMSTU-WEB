using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Project.Core.Exceptions;
using Project.Core.Models.Education;
using Project.Core.Repositories;
using Project.Services.EducationService;
using Xunit;

namespace Project.Service.Tests;

/// <summary>
/// Лондонские (mock-based) тесты для EducationService.
/// Все зависимости замокированы — тесты проверяют логику сервиса в изоляции.
/// </summary>
public class EducationServiceTests
{
    private readonly Mock<IEducationRepository> _repositoryMock;
    private readonly EducationService _service;
    private readonly Guid _employeeId = Guid.NewGuid();

    public EducationServiceTests()
    {
        _repositoryMock = new Mock<IEducationRepository>(MockBehavior.Strict);
        _service = new EducationService(
            _repositoryMock.Object,
            NullLogger<EducationService>.Instance);
    }

    // ===== AddEducationAsync =====

    [Fact]
    public async Task AddEducationAsync_ValidData_ReturnsCreatedEducation()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var expected = new BaseEducation(
            expectedId, _employeeId, "МГУ", "Высшее (бакалавриат)",
            "Информатика", new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30));

        _repositoryMock
            .Setup(r => r.AddEducationAsync(It.Is<CreateEducation>(e =>
                e.EmployeeId == _employeeId &&
                e.Institution == "МГУ" &&
                e.StudyField == "Информатика")))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.AddEducationAsync(
            _employeeId, "МГУ", "Высшее (бакалавриат)", "Информатика",
            new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(_employeeId, result.EmployeeId);
        Assert.Equal("МГУ", result.Institution);
        Assert.Equal(EducationLevel.Bachelor, result.Level);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task AddEducationAsync_InvalidDates_ThrowsArgumentException()
    {
        // Arrange — startDate > endDate → конструктор CreateEducation бросит исключение
        var startDate = new DateOnly(2022, 9, 1);
        var endDate = new DateOnly(2020, 6, 30);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddEducationAsync(
                _employeeId, "МГУ", "Высшее (бакалавриат)", "Информатика",
                startDate, endDate));

        // Мок не должен быть вызван — исключение возникло до обращения к репозиторию
        _repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddEducationAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange — репозиторий бросает EducationAlreadyExistsException
        _repositoryMock
            .Setup(r => r.AddEducationAsync(It.IsAny<CreateEducation>()))
            .ThrowsAsync(new EducationAlreadyExistsException("duplicate"));

        // Act & Assert
        await Assert.ThrowsAsync<EducationAlreadyExistsException>(() =>
            _service.AddEducationAsync(
                _employeeId, "МГУ", "Высшее (бакалавриат)", "Информатика",
                new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30)));

        _repositoryMock.VerifyAll();
    }

    // ===== GetEducationByIdAsync =====

    [Fact]
    public async Task GetEducationByIdAsync_Existing_ReturnsEducation()
    {
        // Arrange
        var educationId = Guid.NewGuid();
        var expected = new BaseEducation(
            educationId, _employeeId, "МГУ", "Высшее (бакалавриат)",
            "Информатика", new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30));

        _repositoryMock
            .Setup(r => r.GetEducationByIdAsync(educationId))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetEducationByIdAsync(educationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(educationId, result.Id);
        Assert.Equal("МГУ", result.Institution);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetEducationByIdAsync_NonExistent_ThrowsEducationNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetEducationByIdAsync(nonExistentId))
            .ThrowsAsync(new EducationNotFoundException($"not found"));

        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _service.GetEducationByIdAsync(nonExistentId));

        _repositoryMock.VerifyAll();
    }

    // ===== UpdateEducationAsync =====

    [Fact]
    public async Task UpdateEducationAsync_ValidData_ReturnsUpdatedEducation()
    {
        // Arrange
        var educationId = Guid.NewGuid();
        var updated = new BaseEducation(
            educationId, _employeeId, "СПбГУ", "Высшее (бакалавриат)",
            "Информатика", new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30));

        _repositoryMock
            .Setup(r => r.UpdateEducationAsync(It.Is<UpdateEducation>(e =>
                e.Id == educationId &&
                e.EmployeeId == _employeeId &&
                e.Institution == "СПбГУ")))
            .ReturnsAsync(updated);

        // Act
        var result = await _service.UpdateEducationAsync(
            educationId, _employeeId, "СПбГУ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("СПбГУ", result.Institution);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateEducationAsync_NonExistent_ThrowsEducationNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.UpdateEducationAsync(It.Is<UpdateEducation>(e =>
                e.Id == nonExistentId)))
            .ThrowsAsync(new EducationNotFoundException("not found"));

        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _service.UpdateEducationAsync(nonExistentId, _employeeId, "Новый вуз"));

        _repositoryMock.VerifyAll();
    }

    // ===== GetEducationsByEmployeeIdAsync =====

    [Fact]
    public async Task GetEducationsByEmployeeIdAsync_WithData_ReturnsEducations()
    {
        // Arrange
        var educations = new List<BaseEducation>
        {
            new BaseEducation(Guid.NewGuid(), _employeeId, "МГУ", "Высшее (бакалавриат)",
                "Информатика", new DateOnly(2018, 9, 1), new DateOnly(2022, 6, 30)),
            new BaseEducation(Guid.NewGuid(), _employeeId, "МФТИ", "Высшее (магистратура)",
                "Физика", new DateOnly(2019, 9, 1), new DateOnly(2021, 6, 30))
        };

        _repositoryMock
            .Setup(r => r.GetEducationsAsync(_employeeId, 1, 10))
            .ReturnsAsync(educations);

        // Act
        var result = await _service.GetEducationsByEmployeeIdAsync(_employeeId, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetEducationsByEmployeeIdAsync_NoData_ReturnsEmptyList()
    {
        // Arrange
        var unknownEmployeeId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetEducationsAsync(unknownEmployeeId, 1, 10))
            .ReturnsAsync(new List<BaseEducation>());

        // Act
        var result = await _service.GetEducationsByEmployeeIdAsync(unknownEmployeeId, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _repositoryMock.VerifyAll();
    }

    // ===== DeleteEducationAsync =====

    [Fact]
    public async Task DeleteEducationAsync_Existing_DeletesSuccessfully()
    {
        // Arrange
        var educationId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.DeleteEducationAsync(educationId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteEducationAsync(educationId);

        // Assert — нет исключения, мок был вызван
        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteEducationAsync_NonExistent_ThrowsEducationNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.DeleteEducationAsync(nonExistentId))
            .ThrowsAsync(new EducationNotFoundException("not found"));

        // Act & Assert
        await Assert.ThrowsAsync<EducationNotFoundException>(() =>
            _service.DeleteEducationAsync(nonExistentId));

        _repositoryMock.VerifyAll();
    }
}
