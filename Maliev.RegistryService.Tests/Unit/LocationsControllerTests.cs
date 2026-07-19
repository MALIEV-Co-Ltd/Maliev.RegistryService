using Maliev.RegistryService.Api.Controllers;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class LocationsControllerTests
{
    private readonly Mock<IThaiRegistryService> _mockService;
    private readonly LocationsController _controller;

    public LocationsControllerTests()
    {
        _mockService = new Mock<IThaiRegistryService>();
        _controller = new LocationsController(_mockService.Object);
    }

    [Fact]
    public async Task Autocomplete_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Autocomplete(null!, 10);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ThaiLocation>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Autocomplete_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Autocomplete("   ", 10);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ThaiLocation>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Autocomplete_WithValidQuery_ReturnsOk()
    {
        // Arrange
        var locations = new List<ThaiLocation>
        {
            new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" }
        };
        _mockService.Setup(s => s.AutocompleteAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.Autocomplete("Bangkok", 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ThaiLocation>>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task AutocompleteMultiField_WithAllEmptyParams_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.AutocompleteMultiField(null, null, null, null, 3);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ThaiLocation>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task AutocompleteMultiField_WithPostalCode_ReturnsOk()
    {
        // Arrange
        var locations = new List<ThaiLocation>
        {
            new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" }
        };
        _mockService.Setup(s => s.AutocompleteMultiFieldAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.AutocompleteMultiField("10100", null, null, null, 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ThaiLocation>>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ThaiLocation?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var location = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" };
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(location);

        // Act
        var result = await _controller.GetById(location.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WithIdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var location = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" };

        // Act
        var result = await _controller.Update(Guid.NewGuid(), location);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var location = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" };
        _mockService.Setup(s => s.UpdateAsync(It.IsAny<ThaiLocation>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(location.Id, location);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        // Arrange
        var location = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" };
        _mockService.Setup(s => s.UpdateAsync(It.IsAny<ThaiLocation>()))
            .ReturnsAsync(true);
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(location);

        // Act
        var result = await _controller.Update(location.Id, location);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        // Arrange
        var location = new ThaiLocation { Id = Guid.NewGuid(), PostalCode = "10100" };
        _mockService.Setup(s => s.CreateAsync(It.IsAny<ThaiLocation>()))
            .ReturnsAsync(location);

        // Act
        var result = await _controller.Create(location);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThaiLocation>>(createdResult.Value);
        Assert.True(apiResponse.Success);
    }
}
