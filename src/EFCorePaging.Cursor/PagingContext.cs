using EFCorePaging.Cursor.Enums;
using Paging.Common.Enums;

namespace EFCorePaging.Cursor;

public record PagingContext(
    int PageSize,
    KeysetCursor? Cursor,
    List<(string PropertyName, SortDirection Direction)> SortColumns,
    string PrimaryKeyColumn,
    NavigationDirection NavigationDirection = NavigationDirection.Forward);