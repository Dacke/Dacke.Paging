using Paging.Common.Enums;
using Paging.Common.Filtering;

namespace DapperPaging.Offset;

public class PagedRequest {
    /// <summary>
    /// Number of records to skip
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Requested record count per page
    /// </summary>
    public virtual int PageSize { get; set; } = 20;

    /// <summary>
    /// Comma-separated string of properties to sort by.
    /// </summary>
    /// <remarks>
    /// Values like "LastName, FirstName" are sorted ascending.  
    /// You can also mix directions such as "LastName desc, FirstName asc" if desired.
    /// </remarks>
    public virtual string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Sets the default sort direction.
    /// </summary>
    /// <remarks>asc or desc</remarks>
    public virtual SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

    #region Filter Configuration

    /// <summary>
    /// Specifies how multiple global filters are combined.
    /// </summary>
    public FilterOperator GlobalFilterOperator { get; set; } = FilterOperator.All;

    /// <summary>
    /// Collection of filters to apply to the requested data.
    /// </summary>
    public List<FilterRequest> Filters { get; set; } = [];

    #endregion
}