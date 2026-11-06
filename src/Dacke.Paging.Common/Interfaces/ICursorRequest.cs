using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.Common.Interfaces;

public interface ICursorRequest {
    /// <summary>
    /// Opaque Base64 string containing encoded KeysetCursor JSON metadata.
    /// Null for the initial page request.
    /// </summary>
    string? Cursor { get; set; }

    NavigationDirection NavigationDirection { get; set; }

    /// <summary>
    /// Requested record count per segment
    /// </summary>
    int PageSize { get; set; }
}