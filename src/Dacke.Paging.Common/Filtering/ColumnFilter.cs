using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.Common.Filtering;

public class ColumnFilter {
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// PrimeNG string match modes: "startsWith", "contains", "notContains", 
    /// "endsWith", "equals", "notEquals", "gt", "gte", "lt", "lte"
    /// </summary>
    public MatchMode MatchMode { get; set; } = MatchMode.Equals;

    public string Value { get; set; } = string.Empty;
}