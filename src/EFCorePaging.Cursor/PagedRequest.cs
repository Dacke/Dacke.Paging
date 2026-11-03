using EFCorePaging.Cursor.Enums;
using Paging.Common.Enums;
using Paging.Common.Filtering;

namespace EFCorePaging.Cursor;

public class PagedRequest {
    /// <summary>
    /// Opaque Base64 string containing encoded KeysetCursor JSON metadata.
    /// Null for the initial page request.
    /// </summary>
    public string? Cursor { get; set; }

    public NavigationDirection NavigationDirection { get; set; } = NavigationDirection.Forward;
    
    /// <summary>
    /// Requested record count per segment (defaults to 20).
    /// </summary>
    public virtual int PageSize { get; set; } = 20;

    /// <summary>
    /// Name of the main entity property to sort by (e.g., "LastName", "Age", "State").
    /// </summary>
    public virtual string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Ascending or Descending sort direction.
    /// </summary>
    public virtual SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Specifies how multiple global filters are combined.
    /// </summary>
    public FilterOperator GlobalFilterOperator { get; set; } = FilterOperator.All;

    /// <summary>
    /// Collection of filters to apply to the requested data.
    /// </summary>
    public List<FilterRequest> Filters { get; set; } = new();
}