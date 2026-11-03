using Dacke.EFCorePaging.Cursor.Enums;
using Dacke.Paging.Common.Enums;

namespace Dacke.EFCorePaging.Cursor;

public record PagingContext(
    int PageSize,
    KeysetCursor? Cursor,
    List<(string PropertyName, SortDirection Direction)> SortColumns,
    string PrimaryKeyColumn,
    NavigationDirection NavigationDirection = NavigationDirection.Forward);