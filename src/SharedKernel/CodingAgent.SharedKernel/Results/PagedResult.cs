namespace CodingAgent.SharedKernel.Results;

/// <summary>
/// Represents a paginated result set with metadata
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
    }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 50)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}
