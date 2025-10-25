namespace CodingAgent.SharedKernel.Results;

/// <summary>
/// Pagination parameters for querying data
/// </summary>
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Page number (1-based). Minimum: 1, Default: 1
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page. Minimum: 1, Maximum: 100, Default: 50
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>
    /// Calculate the number of items to skip
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Number of items to take
    /// </summary>
    public int Take => PageSize;

    public PaginationParameters()
    {
    }

    public PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
