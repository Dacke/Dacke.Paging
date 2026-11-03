using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.Common.Filtering;

public class FilterRequest {
    /// <summary>
    /// Global or Column-level match operator ("all" = AND, "any" = OR).
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.All;

    /// <summary>
    /// Rules per field name (e.g., "country", "name", "age").
    /// </summary>
    public List<ColumnFilter> Constraints { get; set; } = [];
}