using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Helpers;
using Dacke.Paging.Common.Interfaces;
using Dacke.Paging.Common.Sorting;

namespace Dacke.Paging.Common.Extensions;

public static class CursorPagingExtensions {
    public static PagingContext BuildPagingContext<TRequest>(this TRequest req, PagingMetadata pagingMetadata)
        where TRequest : ICursorRequest, ISortableRequest {
        pagingMetadata.Validate();

        int pageSize = req.PageSize is < 1 or > 1000 ? 20 : req.PageSize;
        var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
        var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.DefaultSortDirection, defaultColumn: pagingMetadata.DefaultSortColumn);

        var pagingContext = new PagingContext(pageSize, cursor, sortColumns, pagingMetadata.EntityKeyColumn, req.NavigationDirection);

        return pagingContext;
    }
}