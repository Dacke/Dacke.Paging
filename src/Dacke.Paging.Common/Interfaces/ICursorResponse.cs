namespace Dacke.Paging.Common.Interfaces;

public interface ICursorResponse<T> {
    /// <summary>
    /// The records for the current page that match the filter criteria.
    /// </summary>
    T[] Values { get; set; }

    /// <summary>
    /// Indicates if there is more data to get.
    /// </summary>
    bool HasMore { get; set; }

    /// <summary>
    /// The cursor that will retrieve the next page of records
    /// </summary>
    string? NextCursor { get; set; }

    /// <summary>
    /// Indicates if there is a previous page of data
    /// </summary>
    bool HasPrevious { get; set; }

    /// <summary>
    /// The cursor that will allow you to retrieve the pervious page of records.
    /// </summary>
    string? PreviousCursor { get; set; }
}