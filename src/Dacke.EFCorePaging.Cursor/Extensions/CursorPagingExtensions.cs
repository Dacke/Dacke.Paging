using Dacke.EFCorePaging.Cursor.Enums;
using Dacke.EFCorePaging.Cursor.Helpers;
using Dacke.Paging.Common.Sorting;

namespace Dacke.EFCorePaging.Cursor.Extensions;

public static class CursorPagingExtensions {
    public static PagingContext BuildPagingContext(this PagedRequest req, PagingMetadata pagingMetadata) {
        pagingMetadata.Validate();

        int pageSize = req.PageSize is < 1 or > 1000 ? 20 : req.PageSize;
        var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
        var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.DefaultSortDirection, defaultColumn: pagingMetadata.DefaultSortColumn);

        var pagingContext = new PagingContext(pageSize, cursor, sortColumns, pagingMetadata.EntityKeyColumn, req.NavigationDirection);

        return pagingContext;
    }
}

public static class CursorPagedResponseBuilder {
    public static PagedResponse<TResponse> BuildResponse<TResponse>(List<TResponse> results, PagingContext pagingContext) {
        bool isBackward = pagingContext.NavigationDirection == NavigationDirection.Backward;
        bool hasBoundaryMatch = results.Count > pagingContext.PageSize;

        if (hasBoundaryMatch)
            results.RemoveAt(results.Count - 1);

        if (isBackward)
            results = [.. Enumerable.Reverse(results)];

        bool hasMore = isBackward ? (pagingContext.Cursor != null) : hasBoundaryMatch;
        bool hasPrevious = isBackward ? hasBoundaryMatch : (pagingContext.Cursor != null);

        string? nextCursor = BuildCursorForBoundary(hasMore, pagingContext.SortColumns, results, isLastItem: true);
        string? previousCursor = BuildCursorForBoundary(hasPrevious, pagingContext.SortColumns, results, isLastItem: false);

        var response = new PagedResponse<PersonDto>(results, nextCursor, previousCursor, hasMore, hasPrevious);
    }
}