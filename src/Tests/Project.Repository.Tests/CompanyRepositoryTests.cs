using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.Company;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class CompanyRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly CompanyRepository _repository;

    public CompanyRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new CompanyRepository(
            _fixture.Context,
            NullLogger<CompanyRepository>.Instance);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddCompanyAsync =====
    
    [Fact]
    public async Task AddCompanyAsync_ShouldAddNewCompany()
    {
        // Arrange
        var newCompany = CompanyDbObjectFabric.CreateValidCreationCompany("ООО Тестовая");

        // Act
        var result = await _repository.AddCompanyAsync(newCompany);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.CompanyId);
        Assert.Equal(newCompany.Title, result.Title);

        var dbCompany = await _fixture.Context.CompanyDb
            .FirstOrDefaultAsync(c => c.Id == result.CompanyId);
        Assert.NotNull(dbCompany);
    }

    [Fact]
    public async Task AddCompanyAsync_ShouldThrowWhenCompanyExists()
    {
        // Arrange
        var first = CompanyDbObjectFabric.CreateValidCreationCompany();
        await _repository.AddCompanyAsync(first);

        var duplicate = new CreationCompany(
            first.Title,
            first.RegistrationDate,
            first.PhoneNumber,
            first.Email,
            first.Inn,
            first.Kpp,
            first.Ogrn,
            "Другой адрес");

        // Act & Assert
        await Assert.ThrowsAsync<CompanyAlreadyExistsException>(() => 
            _repository.AddCompanyAsync(duplicate));
    }

    // ===== UpdateCompanyAsync =====

    [Fact]
    public async Task UpdateCompanyAsync_ShouldUpdateExistingCompany()
    {
        // Arrange
        var creation = CompanyDbObjectFabric.CreateValidCreationCompany();
        var created = await _repository.AddCompanyAsync(creation);

        var updateModel = CompanyDbObjectFabric.CreateValidUpdateCompany(
            created.CompanyId, "Обновлённая компания");

        // Act
        var result = await _repository.UpdateCompanyAsync(updateModel);

        // Assert
        Assert.Equal(updateModel.CompanyId, result.CompanyId);
        Assert.Equal(updateModel.Title, result.Title);
        Assert.Equal(updateModel.PhoneNumber, result.PhoneNumber);

        using var freshCtx = _fixture.CreateFreshContext();
        var updatedCompany = await freshCtx.CompanyDb
            .FirstOrDefaultAsync(c => c.Id == created.CompanyId);
        Assert.NotNull(updatedCompany);
        Assert.Equal(updateModel.Title, updatedCompany!.Title);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ShouldThrowWhenCompanyNotFound()
    {
        // Arrange
        var updateModel = CompanyDbObjectFabric.CreateValidUpdateCompany(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<CompanyNotFoundException>(() => 
            _repository.UpdateCompanyAsync(updateModel));
    }

    // ===== GetCompanyByIdAsync =====

    [Fact]
    public async Task GetCompanyByIdAsync_ShouldReturnCompany()
    {
        // Arrange
        var creation = CompanyDbObjectFabric.CreateValidCreationCompany();
        var created = await _repository.AddCompanyAsync(creation);

        // Act
        var result = await _repository.GetCompanyByIdAsync(created.CompanyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.CompanyId, result.CompanyId);
        Assert.Equal(created.Title, result.Title);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ShouldThrowWhenCompanyNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<CompanyNotFoundException>(() => 
            _repository.GetCompanyByIdAsync(Guid.NewGuid()));
    }

    // ===== DeleteCompanyAsync =====

    [Fact]
    public async Task DeleteCompanyAsync_ShouldSoftDeleteCompany()
    {
        // Arrange
        var creation = CompanyDbObjectFabric.CreateValidCreationCompany();
        var created = await _repository.AddCompanyAsync(creation);

        _fixture.Context.ChangeTracker.Clear();
        
        // Act
        await _repository.DeleteCompanyAsync(created.CompanyId);

        // Assert — soft delete: IsDeleted = true
        using var freshCtx = _fixture.CreateFreshContext();
        var deletedCompany = await freshCtx.CompanyDb
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == created.CompanyId);
        Assert.NotNull(deletedCompany);
        Assert.True(deletedCompany!.IsDeleted);
    }

    [Fact]
    public async Task DeleteCompanyAsync_ShouldNotThrowWhenCompanyNotFound()
    {
        // Act
        var exception = await Record.ExceptionAsync(() => 
            _repository.DeleteCompanyAsync(Guid.NewGuid()));

        // Assert
        Assert.Null(exception);
    }

    // ===== GetCompaniesAsync =====

    [Fact]
    public async Task GetCompaniesAsync_ShouldReturnAllCompanies()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var c = CompanyDbObjectFabric.CreateValidCreationCompany();
            await _repository.AddCompanyAsync(c);
        }

        // Act
        var result = await _repository.GetCompaniesAsync();

        // Assert
        var companies = result.ToList();
        Assert.Equal(5, companies.Count);
    }

    [Fact]
    public async Task GetCompaniesAsync_EmptyDb_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetCompaniesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
