namespace Dacke.EFCorePaging.Cursor;

public class PagedResponse<T> {
    /// <summary>
    /// The records for the current page that match the filter criteria.
    /// </summary>
    public T[] Values { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Indicates if there is more data to get.
    /// </summary>
    public bool HasMore { get; set; }
    /// <summary>
    /// The cursor that will retrieve the next page of records
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Indicates if there is a previous page of data
    /// </summary>
    public bool HasPrevious { get; set; }
    /// <summary>
    /// The cursor that will allow you to retrieve the pervious page of records.
    /// </summary>
    public string? PreviousCursor { get; set; }
    
    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> values, string? nextCursor = null, string? previousCursor = null, bool hasMore = false, bool hasPrevious = false) {
        Values = values.ToArray();

        HasMore = hasMore;
        NextCursor = nextCursor;

        HasPrevious = hasPrevious;
        PreviousCursor = previousCursor;
    }
}