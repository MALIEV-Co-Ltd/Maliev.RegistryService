using Maliev.RegistryService.Api.Controllers;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class CompaniesControllerTests
{
    private readonly Mock<IDbdProxyService> _mockDbdService;
    private readonly Mock<ILogger<CompaniesController>> _mockLogger;
    private readonly CompaniesController _controller;

    public CompaniesControllerTests()
    {
        _mockDbdService = new Mock<IDbdProxyService>();
        _mockLogger = new Mock<ILogger<CompaniesController>>();
        _controller = new CompaniesController(_mockDbdService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Search_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(null!, 10);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("   ", 10);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("test", 0);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitOver100_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("test", 101);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOk()
    {
        // Arrange
        var companies = new List<CompanyProfile>
        {
            new CompanyProfile("1", "Active", "1234567890123", "Test Company", "Business", "5", null, "Full Name")
        };
        _mockDbdService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.Search("1234567890123", 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitAppliesLimitToResults()
    {
        // Arrange
        var companies = Enumerable.Range(1, 20).Select(i =>
            new CompanyProfile("1", "Active", $"{i:D13}", $"Company {i}", "Business", "5", null, $"Full {i}"));
        _mockDbdService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);

        // Act
        var result = await _controller.Search("test", 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(5, apiResponse.Data!.Count());
    }

    [Fact]
    public async Task Search_WhenServiceThrowsInvalidOperationException_ReturnsServiceUnavailable()
    {
        // Arrange
        _mockDbdService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var result = await _controller.Search("test", 10);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, statusResult.StatusCode);
    }

    [Fact]
    public async Task Search_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        _mockDbdService.Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act
        var result = await _controller.Search("test", 10);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
