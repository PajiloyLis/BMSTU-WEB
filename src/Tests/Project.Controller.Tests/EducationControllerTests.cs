using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Project.Controller.Tests.Factories;
using Project.Controller.Tests.Fixtures;
using Project.Core.Exceptions;
using Project.Core.Models.Education;
using Project.Core.Services;
using Project.Dto.Http.Education;
using Project.HttpServer.Controllers;
using Xunit;

namespace Project.Controller.Tests;

public class EducationControllerTests
{
    private readonly Mock<IEducationService> _serviceMock = new();
    private readonly EducationController _controller;

    public EducationControllerTests()
    {
        var fixture = new ControllerFixture();
        _controller = new EducationController(fixture.CreateLogger<EducationController>().Object, _serviceMock.Object);
    }

    [Fact]
    public async Task GetEducation_WhenExists_ReturnsOk()
    {
        var educationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var education = EducationObjectFabric.BaseEducation(educationId, employeeId);
        _serviceMock.Setup(x => x.GetEducationByIdAsync(educationId)).ReturnsAsync(education);

        var result = await _controller.GetEducation(educationId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EducationDto>(ok.Value);
        Assert.Equal(educationId, dto.Id);
    }

    [Fact]
    public async Task CreateEducation_WhenDuplicate_ReturnsBadRequest()
    {
        var request = EducationObjectFabric.CreateEducationDto(Guid.NewGuid());
        _serviceMock.Setup(x => x.AddEducationAsync(
                request.EmployeeId,
                request.Institution,
                request.Level,
                request.StudyField,
                request.StartDate,
                request.EndDate))
            .ThrowsAsync(new EducationAlreadyExistsException("duplicate"));

        var result = await _controller.CreateEducation(request);

        ControllerFixture.AssertError(result, StatusCodes.Status400BadRequest, nameof(EducationAlreadyExistsException));
    }

    [Fact]
    public async Task UpdateEducation_WhenValid_ReturnsOk()
    {
        var educationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var request = EducationObjectFabric.UpdateEducationDto(employeeId, "MIPT");
        var updated = EducationObjectFabric.BaseEducation(educationId, employeeId, "MIPT");
        _serviceMock.Setup(x => x.UpdateEducationAsync(
                educationId,
                request.EmployeeId,
                request.Institution,
                request.Level,
                request.StudyField,
                request.StartDate,
                request.EndDate))
            .ReturnsAsync(updated);

        var result = await _controller.UpdateEducation(educationId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EducationDto>(ok.Value);
        Assert.Equal("MIPT", dto.Institution);
    }

    [Fact]
    public async Task DeleteEducation_WhenMissing_ReturnsNotFound()
    {
        var educationId = Guid.NewGuid();
        _serviceMock.Setup(x => x.DeleteEducationAsync(educationId))
            .ThrowsAsync(new EducationNotFoundException("missing"));

        var result = await _controller.DeleteEducation(educationId);

        ControllerFixture.AssertError(result, StatusCodes.Status404NotFound, nameof(EducationNotFoundException));
    }

    [Fact]
    public async Task GetEducationsByEmployeeId_UsesPaginationAndReturnsList()
    {
        var employeeId = Guid.NewGuid();
        var educations = new List<BaseEducation>
        {
            EducationObjectFabric.BaseEducation(Guid.NewGuid(), employeeId),
            EducationObjectFabric.BaseEducation(Guid.NewGuid(), employeeId, "HSE")
        };
        _serviceMock.Setup(x => x.GetEducationsByEmployeeIdAsync(employeeId, 2, 5))
            .ReturnsAsync(educations);

        var result = await _controller.GetEducationsByEmployeeId(employeeId, 2, 5);

        _serviceMock.Verify(x => x.GetEducationsByEmployeeIdAsync(employeeId, 2, 5), Times.Once);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<EducationDto>>(ok.Value);
        Assert.Equal(2, dto.Count());
    }
}

