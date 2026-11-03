namespace Dacke.EFCorePaging.Offset;

public class PagedResponse<T> {
    /// <summary>
    /// Total number of records matching the filter criteria.
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// The records for the current page that match the filter criteria.
    /// </summary>
    public T[] Values { get; set; } = [];

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> values, int totalCount) {
        Values = [.. values];
        TotalCount = totalCount;
    }
}