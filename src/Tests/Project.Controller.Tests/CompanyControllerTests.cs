using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Project.Controller.Tests.Factories;
using Project.Controller.Tests.Fixtures;
using Project.Core.Exceptions;
using Project.Core.Models.Company;
using Project.Core.Services;
using Project.Dto.Http.Company;
using Project.HttpServer.Controllers;
using Xunit;

namespace Project.Controller.Tests;

public class CompanyControllerTests
{
    private readonly Mock<ICompanyService> _serviceMock = new();
    private readonly CompanyController _controller;

    public CompanyControllerTests()
    {
        var fixture = new ControllerFixture();
        _controller = new CompanyController(fixture.CreateLogger<CompanyController>().Object, _serviceMock.Object);
    }

    [Fact]
    public async Task GetCompany_WhenExists_ReturnsOk()
    {
        var companyId = Guid.NewGuid();
        var company = CompanyObjectFabric.BaseCompany(companyId);
        _serviceMock.Setup(x => x.GetCompanyByIdAsync(companyId)).ReturnsAsync(company);

        var result = await _controller.GetCompany(companyId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CompanyDto>(ok.Value);
        Assert.Equal(companyId, dto.CompanyId);
    }

    [Fact]
    public async Task GetCompany_WhenMissing_ReturnsNotFound()
    {
        var companyId = Guid.NewGuid();
        _serviceMock.Setup(x => x.GetCompanyByIdAsync(companyId))
            .ThrowsAsync(new CompanyNotFoundException("not found"));

        var result = await _controller.GetCompany(companyId);

        ControllerFixture.AssertError(result, StatusCodes.Status404NotFound, nameof(CompanyNotFoundException));
    }

    [Fact]
    public async Task CreateCompany_WhenValid_ReturnsCreated()
    {
        var request = CompanyObjectFabric.CreateCompanyDto();
        var created = CompanyObjectFabric.BaseCompany(Guid.NewGuid(), request.Title);
        _serviceMock.Setup(x => x.AddCompanyAsync(
                request.Title,
                request.RegistrationDate,
                request.PhoneNumber,
                request.Email,
                request.Inn,
                request.Kpp,
                request.Ogrn,
                request.Address))
            .ReturnsAsync(created);

        var result = await _controller.CreateCompany(request);

        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var dto = Assert.IsType<CompanyDto>(createdResult.Value);
        Assert.Equal(created.CompanyId, dto.CompanyId);
    }

    [Fact]
    public async Task UpdateCompany_WhenValid_ReturnsOk()
    {
        var companyId = Guid.NewGuid();
        var request = CompanyObjectFabric.UpdateCompanyDto("Renamed");
        var updated = CompanyObjectFabric.BaseCompany(companyId, "Renamed");
        _serviceMock.Setup(x => x.UpdateCompanyAsync(
                companyId,
                request.Title,
                request.RegistrationDate,
                request.PhoneNumber,
                request.Email,
                request.Inn,
                request.Kpp,
                request.Ogrn,
                request.Address))
            .ReturnsAsync(updated);

        var result = await _controller.UpdateCompany(companyId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CompanyDto>(ok.Value);
        Assert.Equal("Renamed", dto.Title);
    }

    [Fact]
    public async Task GetCompanies_ReturnsAll()
    {
        var companies = new List<BaseCompany>
        {
            CompanyObjectFabric.BaseCompany(Guid.NewGuid(), "A"),
            CompanyObjectFabric.BaseCompany(Guid.NewGuid(), "B")
        };
        _serviceMock.Setup(x => x.GetCompaniesAsync()).ReturnsAsync(companies);

        var result = await _controller.GetCompanies();

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<CompanyDto>>(ok.Value);
        Assert.Equal(2, dto.Count());
    }
}

