namespace Maliev.RegistryService.Api.Infrastructure;

/// <summary>
/// Represents a standardized API response.
/// </summary>
/// <typeparam name="T">The type of the data returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the data returned by the request.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the message associated with the response.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the errors associated with the response.
    /// </summary>
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    /// <param name="data">The data to include in the response.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> CreateSuccess(T data) => new() { Success = true, Data = data };

    /// <summary>
    /// Creates an erroneous API response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errors">An optional collection of detailed errors.</param>
    /// <returns>An erroneous API response.</returns>
    public static ApiResponse<T> CreateError(string message, IEnumerable<string>? errors = null) => new() { Success = false, Message = message, Errors = errors };
}
