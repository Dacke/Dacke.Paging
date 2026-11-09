namespace Dacke.Paging.Common.Interfaces;

public interface IOffsetRequest {
    /// <summary>
    /// Number of records to skip
    /// </summary>
    int Skip { get; set; }

    /// <summary>
    /// Requested record count per page
    /// </summary>
    int PageSize { get; set; }
}