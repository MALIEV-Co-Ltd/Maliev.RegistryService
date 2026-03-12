using Maliev.RegistryService.Api.Infrastructure;
using Xunit;

namespace Maliev.RegistryService.Tests.Unit;

public class ApiResponseTests
{
    [Fact]
    public void CreateSuccess_SetsSuccessTrue()
    {
        var response = ApiResponse<string>.CreateSuccess("test data");
        Assert.True(response.Success);
        Assert.Equal("test data", response.Data);
    }

    [Fact]
    public void CreateSuccess_WithNullData_SetsNull()
    {
        var response = ApiResponse<string>.CreateSuccess(null!);
        Assert.True(response.Success);
        Assert.Null(response.Data);
    }

    [Fact]
    public void CreateError_SetsSuccessFalse()
    {
        var response = ApiResponse<string>.CreateError("Error message");
        Assert.False(response.Success);
        Assert.Equal("Error message", response.Message);
    }

    [Fact]
    public void CreateError_WithErrors_SetsErrors()
    {
        var errors = new[] { "Error 1", "Error 2" };
        var response = ApiResponse<string>.CreateError("Error message", errors);
        Assert.False(response.Success);
        Assert.Equal("Error message", response.Message);
        Assert.Equal(2, response.Errors!.Count());
    }

    [Fact]
    public void CreateError_WithNullErrors_SetsNullErrors()
    {
        var response = ApiResponse<string>.CreateError("Error message", null);
        Assert.False(response.Success);
        Assert.Null(response.Errors);
    }
}
