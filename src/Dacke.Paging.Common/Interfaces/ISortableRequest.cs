using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.Common.Interfaces;

public interface ISortableRequest {
    /// <summary>
    /// Comma-separated string of properties to sort by.
    /// </summary>
    /// <remarks>
    /// Values like "LastName, FirstName" are sorted ascending.  
    /// You can also mix directions such as "LastName desc, FirstName asc" if desired.
    /// </remarks>
    string SortBy { get; set; }

    /// <summary>
    /// Sets the default sort direction.
    /// </summary>
    /// <remarks>asc or desc</remarks>
    SortDirection DefaultSortDirection { get; set; }
}