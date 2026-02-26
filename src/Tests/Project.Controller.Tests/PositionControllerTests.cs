using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Project.Controller.Tests.Factories;
using Project.Controller.Tests.Fixtures;
using Project.Core.Exceptions;
using Project.Core.Models.Position;
using Project.Core.Services;
using Project.Dto.Http.Position;
using Project.HttpServer.Controllers;
using Xunit;

namespace Project.Controller.Tests;

public class PositionControllerTests
{
    private readonly Mock<IPositionService> _serviceMock = new();
    private readonly PositionController _controller;

    public PositionControllerTests()
    {
        var fixture = new ControllerFixture();
        _controller = new PositionController(fixture.CreateLogger<PositionController>().Object, _serviceMock.Object);
    }

    [Fact]
    public async Task GetPosition_WhenExists_ReturnsOk()
    {
        var positionId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var position = PositionObjectFabric.BasePosition(positionId, companyId, "Developer");
        _serviceMock.Setup(x => x.GetPositionByIdAsync(positionId)).ReturnsAsync(position);

        var result = await _controller.GetPosition(positionId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PositionDto>(ok.Value);
        Assert.Equal(positionId, dto.Id);
    }

    [Fact]
    public async Task CreatePosition_WhenValid_ReturnsCreated()
    {
        var companyId = Guid.NewGuid();
        var request = PositionObjectFabric.CreatePositionDto(companyId, Guid.NewGuid(), "Team Lead");
        var created = PositionObjectFabric.BasePosition(Guid.NewGuid(), companyId, "Team Lead", request.ParentId);
        _serviceMock.Setup(x => x.AddPositionAsync(request.ParentId, request.Title, request.CompanyId))
            .ReturnsAsync(created);

        var result = await _controller.CreatePosition(request);

        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var dto = Assert.IsType<PositionDto>(createdResult.Value);
        Assert.Equal("Team Lead", dto.Title);
    }

    [Fact]
    public async Task UpdatePositionTitle_WhenInvalid_ReturnsBadRequest()
    {
        var positionId = Guid.NewGuid();
        _serviceMock.Setup(x => x.UpdatePositionTitleAsync(positionId, ""))
            .ThrowsAsync(new ArgumentException("invalid title"));

        var result = await _controller.UpdatePositionTitle(positionId, "");

        ControllerFixture.AssertError(result, StatusCodes.Status400BadRequest, nameof(ArgumentException));
    }

    [Fact]
    public async Task DeletePosition_WhenMissing_ReturnsNotFound()
    {
        var positionId = Guid.NewGuid();
        _serviceMock.Setup(x => x.DeletePositionAsync(positionId))
            .ThrowsAsync(new PositionNotFoundException("missing"));

        var result = await _controller.DeletePosition(positionId);

        ControllerFixture.AssertError(result, StatusCodes.Status404NotFound, nameof(PositionNotFoundException));
    }

    [Fact]
    public async Task GetSubordinatesPositionsByHeadPositionId_ReturnsList()
    {
        var headPositionId = Guid.NewGuid();
        var subordinates = new List<PositionHierarchy>
        {
            PositionObjectFabric.PositionHierarchy(Guid.NewGuid(), "Backend", headPositionId),
            PositionObjectFabric.PositionHierarchy(Guid.NewGuid(), "Frontend", headPositionId)
        };
        _serviceMock.Setup(x => x.GetSubordinatesAsync(headPositionId))
            .ReturnsAsync(subordinates);

        var result = await _controller.GetSubordinatesPositionsByHeadPositionId(headPositionId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsAssignableFrom<IEnumerable<PositionHierarchyDto>>(ok.Value);
        Assert.Equal(2, dto.Count());
    }
}

