using Asp.Versioning;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Data.Entities;
using Maliev.RegistryService.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.RegistryService.Api.Controllers;

/// <summary>
/// Controller for location registry lookups.
/// </summary>
[ApiVersion("1.0")]
[Route("registry/v1/thai/addresses")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly IThaiRegistryService _registryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationsController"/> class.
    /// </summary>
    /// <param name="registryService">The Thai registry service.</param>
    public LocationsController(IThaiRegistryService registryService)
    {
        _registryService = registryService;
    }

    /// <summary>
    /// Autocomplete Thai locations by query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>A list of matching Thai locations.</returns>
    [HttpGet("autocomplete")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ThaiLocation>>>> Autocomplete(
        [FromQuery] string query, 
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<IEnumerable<ThaiLocation>>.CreateError("Query parameter is required."));
        }

        var results = await _registryService.AutocompleteAsync(query, limit);
        return Ok(ApiResponse<IEnumerable<ThaiLocation>>.CreateSuccess(results));
    }
}
