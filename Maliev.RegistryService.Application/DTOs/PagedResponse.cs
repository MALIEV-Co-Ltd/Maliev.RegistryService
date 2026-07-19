namespace Maliev.RegistryService.Application.DTOs;

/// <summary>
/// Represents a paged collection returned by a registry query.
/// </summary>
/// <typeparam name="T">The item type contained in the page.</typeparam>
public sealed class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the current one-based page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the requested page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of matching items.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages available for the current query.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
