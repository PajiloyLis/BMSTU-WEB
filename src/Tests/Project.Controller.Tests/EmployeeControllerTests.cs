using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Project.Controller.Tests.Factories;
using Project.Controller.Tests.Fixtures;
using Project.Core.Exceptions;
using Project.Core.Services;
using Project.Dto.Http.Employee;
using Project.HttpServer.Controllers;
using Xunit;

namespace Project.Controller.Tests;

public class EmployeeControllerTests
{
    private readonly Mock<IEmployeeService> _serviceMock = new();
    private readonly EmployeeController _controller;

    public EmployeeControllerTests()
    {
        var fixture = new ControllerFixture();
        _controller = new EmployeeController(fixture.CreateLogger<EmployeeController>().Object, _serviceMock.Object);
    }

    [Fact]
    public async Task GetEmployee_WhenExists_ReturnsOk()
    {
        var employeeId = Guid.NewGuid();
        var employee = EmployeeObjectFabric.BaseEmployee(employeeId);
        _serviceMock.Setup(x => x.GetEmployeeByIdAsync(employeeId)).ReturnsAsync(employee);

        var result = await _controller.GetEmployee(employeeId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EmployeeDto>(ok.Value);
        Assert.Equal(employeeId, dto.EmployeeId);
    }

    [Fact]
    public async Task CreateEmployee_WhenDuplicate_ReturnsBadRequest()
    {
        var request = EmployeeObjectFabric.CreateEmployeeDto();
        _serviceMock.Setup(x => x.AddEmployeeAsync(
                request.FullName,
                request.PhoneNumber,
                request.Email,
                request.Birthday,
                request.PhotoPath,
                request.Duties))
            .ThrowsAsync(new EmployeeAlreadyExistsException("duplicate"));

        var result = await _controller.CreateEmployee(request);

        ControllerFixture.AssertError(result, StatusCodes.Status400BadRequest, nameof(EmployeeAlreadyExistsException));
    }

    [Fact]
    public async Task UpdateEmployee_WhenValid_ReturnsOk()
    {
        var employeeId = Guid.NewGuid();
        var request = EmployeeObjectFabric.UpdateEmployeeDto("Pavel Pavlov");
        var updated = EmployeeObjectFabric.BaseEmployee(employeeId, "Pavel Pavlov");
        _serviceMock.Setup(x => x.UpdateEmployeeAsync(
                employeeId,
                request.FullName,
                request.PhoneNumber,
                request.Email,
                request.Birthday,
                request.PhotoPath,
                request.Duties))
            .ReturnsAsync(updated);

        var result = await _controller.UpdateEmployee(employeeId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EmployeeDto>(ok.Value);
        Assert.Equal("Pavel Pavlov", dto.FullName);
    }

    [Fact]
    public async Task DeleteEmployee_WhenMissing_ReturnsNotFound()
    {
        var employeeId = Guid.NewGuid();
        _serviceMock.Setup(x => x.DeleteEmployeeAsync(employeeId))
            .ThrowsAsync(new EmployeeNotFoundException("missing"));

        var result = await _controller.DeleteEmployee(employeeId);

        ControllerFixture.AssertError(result, StatusCodes.Status404NotFound, nameof(EmployeeNotFoundException));
    }

    [Fact]
    public async Task CreateEmployee_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var request = EmployeeObjectFabric.CreateEmployeeDto();
        _serviceMock.Setup(x => x.AddEmployeeAsync(
                request.FullName,
                request.PhoneNumber,
                request.Email,
                request.Birthday,
                request.PhotoPath,
                request.Duties))
            .ThrowsAsync(new ArgumentException("invalid payload"));

        var result = await _controller.CreateEmployee(request);

        ControllerFixture.AssertError(result, StatusCodes.Status400BadRequest, nameof(ArgumentException));
    }
}

