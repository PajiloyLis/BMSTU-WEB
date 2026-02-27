using Database.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.Company;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.CompanyService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class CompanyServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly CompanyService _companyService;

    public CompanyServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var companyRepository = new CompanyRepository(
            _fixture.Context,
            NullLogger<CompanyRepository>.Instance);

        _companyService = new CompanyService(
            companyRepository,
            NullLogger<CompanyService>.Instance);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddCompanyAsync =====

    [Fact]
    public async Task AddCompanyAsync_ValidData_ReturnsCreatedCompany()
    {
        // Arrange
        var creation = CompanyObjectFabric.CreateValidCreationCompany("Рога и копыта");

        // Act
        var result = await _companyService.AddCompanyAsync(
            creation.Title, creation.RegistrationDate, creation.PhoneNumber,
            creation.Email, creation.Inn, creation.Kpp, creation.Ogrn, creation.Address);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.CompanyId);
        Assert.Equal(creation.Title, result.Title);
        Assert.Equal(creation.Email, result.Email);
        Assert.Equal(creation.Inn, result.Inn);
    }

    [Fact]
    public async Task AddCompanyAsync_DuplicateEmail_ThrowsCompanyAlreadyExistsException()
    {
        // Arrange
        var first = CompanyObjectFabric.CreateValidCreationCompany();
        await _companyService.AddCompanyAsync(
            first.Title, first.RegistrationDate, first.PhoneNumber,
            first.Email, first.Inn, first.Kpp, first.Ogrn, first.Address);

        // Act & Assert
        await Assert.ThrowsAsync<CompanyAlreadyExistsException>(() =>
            _companyService.AddCompanyAsync(
                "Другая компания", first.RegistrationDate, "+79001111111",
                first.Email, "9999999999", "999999999", "9999999999999", "Другой адрес"));
    }

    // ===== GetCompanyByIdAsync =====

    [Fact]
    public async Task GetCompanyByIdAsync_ExistingCompany_ReturnsCompany()
    {
        // Arrange
        var creation = CompanyObjectFabric.CreateValidCreationCompany();
        var created = await _companyService.AddCompanyAsync(
            creation.Title, creation.RegistrationDate, creation.PhoneNumber,
            creation.Email, creation.Inn, creation.Kpp, creation.Ogrn, creation.Address);

        // Act
        var result = await _companyService.GetCompanyByIdAsync(created.CompanyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.CompanyId, result.CompanyId);
        Assert.Equal(created.Title, result.Title);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_NonExistentId_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<CompanyNotFoundException>(() =>
            _companyService.GetCompanyByIdAsync(nonExistentId));
    }

    // ===== UpdateCompanyAsync =====

    [Fact]
    public async Task UpdateCompanyAsync_ValidData_ReturnsUpdatedCompany()
    {
        // Arrange
        var creation = CompanyObjectFabric.CreateValidCreationCompany();
        var created = await _companyService.AddCompanyAsync(
            creation.Title, creation.RegistrationDate, creation.PhoneNumber,
            creation.Email, creation.Inn, creation.Kpp, creation.Ogrn, creation.Address);

        var updatedTitle = "Обновлённая компания";

        // Act
        var result = await _companyService.UpdateCompanyAsync(
            created.CompanyId, updatedTitle, null, null, null, null, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedTitle, result.Title);
        Assert.Equal(created.CompanyId, result.CompanyId);
    }

    [Fact]
    public async Task UpdateCompanyAsync_NonExistentId_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<CompanyNotFoundException>(() =>
            _companyService.UpdateCompanyAsync(nonExistentId, "Новое название",
                null, null, null, null, null, null, null));
    }

    // ===== GetCompaniesAsync =====

    [Fact]
    public async Task GetCompaniesAsync_WithData_ReturnsCompanies()
    {
        // Arrange
        var c1 = CompanyObjectFabric.CreateValidCreationCompany();
        var c2 = CompanyObjectFabric.CreateValidCreationCompany();
        await _companyService.AddCompanyAsync(
            c1.Title, c1.RegistrationDate, c1.PhoneNumber,
            c1.Email, c1.Inn, c1.Kpp, c1.Ogrn, c1.Address);
        await _companyService.AddCompanyAsync(
            c2.Title, c2.RegistrationDate, c2.PhoneNumber,
            c2.Email, c2.Inn, c2.Kpp, c2.Ogrn, c2.Address);

        // Act
        var result = await _companyService.GetCompaniesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    [Fact]
    public async Task GetCompaniesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange — БД уже очищена через ResetDatabaseAsync

        // Act
        var result = await _companyService.GetCompaniesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ===== DeleteCompanyAsync =====

    [Fact]
    public async Task DeleteCompanyAsync_ExistingCompany_DeletesSuccessfully()
    {
        // Arrange
        var creation = CompanyObjectFabric.CreateValidCreationCompany();
        var created = await _companyService.AddCompanyAsync(
            creation.Title, creation.RegistrationDate, creation.PhoneNumber,
            creation.Email, creation.Inn, creation.Kpp, creation.Ogrn, creation.Address);

        // Act
        await _companyService.DeleteCompanyAsync(created.CompanyId);

        // Assert — после soft-delete компания помечается IsDeleted,
        // GetCompanyByIdAsync вернёт её (soft delete), проверяем что не выбрасывает исключение
        var result = await _companyService.GetCompanyByIdAsync(created.CompanyId);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeleteCompanyAsync_NonExistentId_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<CompanyNotFoundException>(() =>
            _companyService.DeleteCompanyAsync(nonExistentId));
    }
}
