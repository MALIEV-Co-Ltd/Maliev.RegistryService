using Asp.Versioning;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Data.Models;
using Maliev.RegistryService.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.RegistryService.Api.Controllers;

/// <summary>
/// Controller for company registry lookups.
/// </summary>
[ApiVersion("1.0")]
[Route("registry/v1/thai/companies")]
[ApiController]
public class CompaniesController : ControllerBase
{
    private readonly IDbdProxyService _dbdService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompaniesController"/> class.
    /// </summary>
    /// <param name="dbdService">The DBD proxy service.</param>
    public CompaniesController(IDbdProxyService dbdService)
    {
        _dbdService = dbdService;
    }

    /// <summary>
    /// Look up company profiles by query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>A list of matching company profiles.</returns>
    [HttpGet("lookup")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "query", "limit" })]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyProfile>>>> Lookup(
        [FromQuery] string query, 
        [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<IEnumerable<CompanyProfile>>.CreateError("Query parameter is required."));
        }

        try
        {
            var results = await _dbdService.LookupAsync(query, limit);
            return Ok(ApiResponse<IEnumerable<CompanyProfile>>.CreateSuccess(results));
        }
        catch (Exception)
        {
            return StatusCode(503, ApiResponse<IEnumerable<CompanyProfile>>.CreateError("Upstream service is currently unavailable."));
        }
    }
}
