using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Project.Controller.Tests.Factories;
using Project.Controller.Tests.Fixtures;
using Project.Core.Exceptions;
using Project.Core.Models.PositionHistory;
using Project.Core.Services;
using Project.Dto.Http.PositionHistory;
using Project.HttpServer.Controllers;
using Xunit;

namespace Project.Controller.Tests;

public class PositionHistoryControllerTests
{
    private readonly Mock<IPositionHistoryService> _serviceMock = new();
    private readonly PositionHistoryController _controller;

    public PositionHistoryControllerTests()
    {
        var fixture = new ControllerFixture();
        _controller = new PositionHistoryController(fixture.CreateLogger<PositionHistoryController>().Object, _serviceMock.Object);
    }

    [Fact]
    public async Task GetPositionHistory_WhenExists_ReturnsOk()
    {
        var positionId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var history = PositionHistoryObjectFabric.BasePositionHistory(positionId, employeeId);
        _serviceMock.Setup(x => x.GetPositionHistoryAsync(positionId, employeeId))
            .ReturnsAsync(history);

        var result = await _controller.GetPositionHistory(employeeId, positionId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PositionHistoryDto>(ok.Value);
        Assert.Equal(positionId, dto.PositionId);
        Assert.Equal(employeeId, dto.EmployeeId);
    }

    [Fact]
    public async Task CreatePositionHistory_WhenInvalid_ReturnsBadRequest()
    {
        var request = PositionHistoryObjectFabric.CreatePositionHistoryDto(Guid.NewGuid(), Guid.NewGuid());
        _serviceMock.Setup(x => x.AddPositionHistoryAsync(
                request.PositionId,
                request.EmployeeId,
                request.StartDate,
                request.EndDate))
            .ThrowsAsync(new ArgumentException("invalid dates"));

        var result = await _controller.CreatePositionHistory(request);

        ControllerFixture.AssertError(result, StatusCodes.Status400BadRequest, nameof(ArgumentException));
    }

    [Fact]
    public async Task UpdatePositionHistory_WhenMissing_ReturnsNotFound()
    {
        var employeeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var request = PositionHistoryObjectFabric.UpdatePositionHistoryDto();
        _serviceMock.Setup(x => x.UpdatePositionHistoryAsync(positionId, employeeId, request.StartDate, request.EndDate))
            .ThrowsAsync(new PositionHistoryNotFoundException("missing"));

        var result = await _controller.UpdatePositionHistory(employeeId, positionId, request);

        ControllerFixture.AssertError(result, StatusCodes.Status404NotFound, nameof(PositionHistoryNotFoundException));
    }

    [Fact]
    public async Task GetPositionHistorysByEmployeeId_UsesDateFilter()
    {
        var employeeId = Guid.NewGuid();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var list = new List<BasePositionHistory>
        {
            PositionHistoryObjectFabric.BasePositionHistory(Guid.NewGuid(), employeeId)
        };
        _serviceMock.Setup(x => x.GetPositionHistoryByEmployeeIdAsync(employeeId, startDate, endDate))
            .ReturnsAsync(list);

        var result = await _controller.GetPositionHistorysByEmployeeId(employeeId, 2, 5, startDate, endDate);

        _serviceMock.Verify(x => x.GetPositionHistoryByEmployeeIdAsync(employeeId, startDate, endDate), Times.Once);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<PositionHistoryDto>>(ok.Value);
        Assert.Single(dto);
    }

    [Fact]
    public async Task GetSubordinatesPositionHistoriesByHeadEmployeeId_UsesPagingAndReturnsList()
    {
        var headEmployeeId = Guid.NewGuid();
        var list = new List<BasePositionHistory>
        {
            PositionHistoryObjectFabric.BasePositionHistory(Guid.NewGuid(), Guid.NewGuid())
        };
        _serviceMock.Setup(x => x.GetCurrentSubordinatesPositionHistoryAsync(headEmployeeId, null, null, 3, 7))
            .ReturnsAsync(list);

        var result = await _controller.GetSubordinatesPositionHistoriesByHeadEmployeeId(headEmployeeId, 3, 7);

        _serviceMock.Verify(x => x.GetCurrentSubordinatesPositionHistoryAsync(headEmployeeId, null, null, 3, 7), Times.Once);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<PositionHistoryDto>>(ok.Value);
        Assert.Single(dto);
    }

    [Fact]
    public async Task GetCurrentSubordinatesByHeadEmployeeId_UsesPagingAndReturnsList()
    {
        var headEmployeeId = Guid.NewGuid();
        var list = new List<PositionHierarchyWithEmployee>
        {
            PositionHistoryObjectFabric.PositionHierarchyWithEmployee(Guid.NewGuid(), Guid.NewGuid())
        };
        _serviceMock.Setup(x => x.GetCurrentSubordinatesAsync(headEmployeeId, 4, 9))
            .ReturnsAsync(list);

        var result = await _controller.GetCurrentSubordinatesByHeadEmployeeId(headEmployeeId, 4, 9);

        _serviceMock.Verify(x => x.GetCurrentSubordinatesAsync(headEmployeeId, 4, 9), Times.Once);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<PositionHierarchyWithEmployeeDto>>(ok.Value);
        Assert.Single(dto);
    }
}

