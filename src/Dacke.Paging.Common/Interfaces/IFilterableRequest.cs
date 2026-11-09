using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Filtering;

namespace Dacke.Paging.Common.Interfaces;

public interface IFilterableRequest {
    /// <summary>
    /// Specifies how multiple global filters are combined.
    /// </summary>
    FilterOperator GlobalFilterOperator { get; set; }

    /// <summary>
    /// Collection of filters to apply to the requested data.
    /// </summary>
    List<FilterRequest> Filters { get; set; }
}