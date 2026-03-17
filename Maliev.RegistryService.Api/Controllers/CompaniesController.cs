using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Infrastructure;
using Maliev.RegistryService.Application.DTOs;
using Maliev.RegistryService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.RegistryService.Api.Controllers;

/// <summary>
/// Controller for Thai company registry lookups via DBD proxy.
/// </summary>
[ApiVersion("1")]
[Route("registry/v{version:apiVersion}/thai/companies")]
[ApiController]
public class CompaniesController : ControllerBase
{
    private readonly IDbdProxyService _dbdProxyService;
    private readonly ILogger<CompaniesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompaniesController"/> class.
    /// </summary>
    /// <param name="dbdProxyService">The DBD proxy service.</param>
    /// <param name="logger">Logger instance.</param>
    public CompaniesController(
        IDbdProxyService dbdProxyService,
        ILogger<CompaniesController> logger)
    {
        _dbdProxyService = dbdProxyService;
        _logger = logger;
    }

    /// <summary>
    /// Search for Thai companies by name or tax ID.
    /// </summary>
    /// <param name="query">The search query (company name or 13-digit tax ID).</param>
    /// <param name="limit">The maximum number of results to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching company profiles.</returns>
    /// <response code="200">Returns the list of matching companies.</response>
    /// <response code="400">If the query parameter is missing or invalid.</response>
    /// <response code="500">If the external service is unavailable.</response>
    [RequirePermission(RegistryPermissions.CompaniesRead)]
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CompanyProfile>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CompanyProfile>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CompanyProfile>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyProfile>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<IEnumerable<CompanyProfile>>.CreateError(
                "Query parameter is required."));
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(ApiResponse<IEnumerable<CompanyProfile>>.CreateError(
                "Limit must be between 1 and 100."));
        }

        try
        {
            var results = await _dbdProxyService.SearchCompaniesAsync(query, cancellationToken);

            // Apply limit on the results
            var limitedResults = results.Take(limit);

            return Ok(ApiResponse<IEnumerable<CompanyProfile>>.CreateSuccess(limitedResults));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error searching companies for query: {Query}", query);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<IEnumerable<CompanyProfile>>.CreateError(
                    "External company registry service is temporarily unavailable. Please try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching companies for query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<CompanyProfile>>.CreateError(
                    "An unexpected error occurred while searching for companies."));
        }
    }
}
