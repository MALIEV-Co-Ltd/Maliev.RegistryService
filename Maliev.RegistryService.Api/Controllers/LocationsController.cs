using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Data.Entities;
using Maliev.RegistryService.Data.Services;
using Microsoft.AspNetCore.Authorization;
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
    [RequirePermission(RegistryPermissions.LocationsRead)]
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

    /// <summary>
    /// Multi-field autocomplete for Thai addresses using composite scoring.
    /// Accepts multiple address fields and returns best matches using weighted similarity.
    /// </summary>
    /// <param name="postalCode">Postal code (5 digits).</param>
    /// <param name="district">Sub-district name (ตำบล/แขวง) in Thai or English.</param>
    /// <param name="city">District/city name (อำเภอ/เขต) in Thai or English.</param>
    /// <param name="province">Province name (จังหวัด) in Thai or English.</param>
    /// <param name="limit">Maximum number of results (default: 3).</param>
    /// <returns>Locations ranked by composite similarity score (postal code 40%, district 30%, city 20%, province 10%).</returns>
    [RequirePermission(RegistryPermissions.LocationsRead)]
    [HttpGet("autocomplete-multi")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ThaiLocation>>>> AutocompleteMultiField(
        [FromQuery] string? postalCode,
        [FromQuery] string? district,
        [FromQuery] string? city,
        [FromQuery] string? province,
        [FromQuery] int limit = 3)
    {
        if (string.IsNullOrWhiteSpace(postalCode) && 
            string.IsNullOrWhiteSpace(district) && 
            string.IsNullOrWhiteSpace(city) && 
            string.IsNullOrWhiteSpace(province))
        {
            return BadRequest(ApiResponse<IEnumerable<ThaiLocation>>.CreateError("At least one field must be provided."));
        }
        
        var results = await _registryService.AutocompleteMultiFieldAsync(
            postalCode, district, city, province, limit);
        
        return Ok(ApiResponse<IEnumerable<ThaiLocation>>.CreateSuccess(results));
    }

    /// <summary>
    /// Get a specific Thai location by ID.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <returns>The matching Thai location.</returns>
    [RequirePermission(RegistryPermissions.LocationsRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ThaiLocation>>> GetById(Guid id)
    {
        var result = await _registryService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(ApiResponse<ThaiLocation>.CreateError("Location not found."));
        }
        return Ok(ApiResponse<ThaiLocation>.CreateSuccess(result));
    }

    /// <summary>
    /// Create a new Thai location.
    /// </summary>
    /// <param name="location">The location to create.</param>
    /// <returns>The created location.</returns>
    [RequirePermission(RegistryPermissions.LocationsCreate)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ThaiLocation>>> Create([FromBody] ThaiLocation location)
    {
        var result = await _registryService.CreateAsync(location);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ThaiLocation>.CreateSuccess(result));
    }

    /// <summary>
    /// Update an existing Thai location.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <param name="location">The updated location data.</param>
    /// <returns>The updated location.</returns>
    [RequirePermission(RegistryPermissions.LocationsUpdate)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ThaiLocation>>> Update(Guid id, [FromBody] ThaiLocation location)
    {
        if (id != location.Id)
        {
            return BadRequest(ApiResponse<ThaiLocation>.CreateError("ID mismatch."));
        }

        var success = await _registryService.UpdateAsync(location);
        if (!success)
        {
            return NotFound(ApiResponse<ThaiLocation>.CreateError("Location not found."));
        }

        // Fetch the fresh record from the database to return to the client
        var updatedLocation = await _registryService.GetByIdAsync(id);
        return Ok(ApiResponse<ThaiLocation>.CreateSuccess(updatedLocation!));
    }

    /// <summary>
    /// Delete a Thai location.
    /// </summary>
    /// <param name="id">The location ID.</param>
    /// <returns>Success indicator.</returns>
    [RequirePermission(RegistryPermissions.LocationsDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var success = await _registryService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse<bool>.CreateError("Location not found."));
        }

        return Ok(ApiResponse<bool>.CreateSuccess(true));
    }
}
