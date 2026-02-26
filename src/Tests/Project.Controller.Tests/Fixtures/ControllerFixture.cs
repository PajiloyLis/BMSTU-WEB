using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Project.Dto.Http;
using Xunit;

namespace Project.Controller.Tests.Fixtures;

public sealed class ControllerFixture
{
    public Mock<ILogger<TController>> CreateLogger<TController>() where TController : class
    {
        return new Mock<ILogger<TController>>();
    }

    public static ObjectResult AssertError(IActionResult result, int expectedStatusCode, string expectedErrorType)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var errorDto = Assert.IsType<ErrorDto>(objectResult.Value);
        Assert.Equal(expectedErrorType, errorDto.ErrorType);
        return objectResult;
    }
}

