using Dacke.EFCorePaging.Cursor.Helpers;
using Dacke.Paging.Common.Enums;
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