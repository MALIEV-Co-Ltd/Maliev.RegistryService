using Maliev.RegistryService.Api.Controllers;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="CompaniesController"/>.
/// </summary>
public class CompaniesControllerTests
{
    private readonly Mock<IThaiCompanyRegistryService> _mockRegistryService;
    private readonly Mock<ILogger<CompaniesController>> _mockLogger;
    private readonly CompaniesController _controller;

    /// <summary>Initializes the controller under test with mocked dependencies.</summary>
    public CompaniesControllerTests()
    {
        _mockRegistryService = new Mock<IThaiCompanyRegistryService>();
        _mockLogger = new Mock<ILogger<CompaniesController>>();
        _controller = new CompaniesController(_mockRegistryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Search_WithNullQuery_ReturnsBadRequest()
    {
        var result = await _controller.Search(null!, 10);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithWhitespaceQuery_ReturnsBadRequest()
    {
        var result = await _controller.Search("   ", 10);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitZero_ReturnsBadRequest()
    {
        var result = await _controller.Search("test", 0);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitOver100_ReturnsBadRequest()
    {
        var result = await _controller.Search("test", 101);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOk()
    {
        var companies = new List<CompanyProfile>
        {
            new CompanyProfile("1", "Active", "1234567890123", "Test Company", "Business", "5", null, "Full Name")
        };
        _mockRegistryService
            .Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);

        var result = await _controller.Search("1234567890123", 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Search_WithLimitAppliesLimitToResults()
    {
        var companies = Enumerable.Range(1, 20).Select(i =>
            new CompanyProfile("1", "Active", $"{i:D13}", $"Company {i}", "Business", "5", null, $"Full {i}"));
        _mockRegistryService
            .Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);

        var result = await _controller.Search("test", 5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<CompanyProfile>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(5, apiResponse.Data!.Count());
    }

    [Fact]
    public async Task Search_WhenServiceThrowsInvalidOperationException_ReturnsServiceUnavailable()
    {
        _mockRegistryService
            .Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var result = await _controller.Search("test", 10);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, statusResult.StatusCode);
    }

    [Fact]
    public async Task Search_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        _mockRegistryService
            .Setup(s => s.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.Search("test", 10);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
