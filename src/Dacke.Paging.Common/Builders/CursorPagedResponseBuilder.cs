using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Helpers;
using Dacke.Paging.Common.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace Dacke.Paging.Common.Builders;

public interface ICursorPagedResponseBuilder {
    TResponse BuildResponse<TResponse, TResult>(List<TResult> results, PagingContext pagingContext, PagingMetadata metadata) 
        where TResponse : ICursorResponse<TResult>, new();
}

public class CursorPagedResponseBuilder : ICursorPagedResponseBuilder {
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    public TResponse BuildResponse<TResponse, TResult>(List<TResult> results, PagingContext pagingContext, PagingMetadata metadata) 
        where TResponse : ICursorResponse<TResult>, new() {
        bool isBackward = pagingContext.NavigationDirection == NavigationDirection.Backward;
        bool hasBoundaryMatch = results.Count > pagingContext.PageSize;

        if (hasBoundaryMatch)
            results.RemoveAt(results.Count - 1);

        if (isBackward)
            results = [.. Enumerable.Reverse(results)];

        bool hasMore = isBackward ? (pagingContext.Cursor != null) : hasBoundaryMatch;
        bool hasPrevious = isBackward ? hasBoundaryMatch : (pagingContext.Cursor != null);

        string? nextCursor = BuildCursorForBoundary(hasMore, pagingContext, metadata, results, isLastItem: true);
        string? previousCursor = BuildCursorForBoundary(hasPrevious, pagingContext, metadata, results, isLastItem: false);

        return new TResponse { 
            Values = [.. results],
            NextCursor = nextCursor,
            PreviousCursor = previousCursor,
            HasMore = hasMore,
            HasPrevious = hasPrevious,
        };
    }


    private string? BuildCursorForBoundary<TResult>(bool canNavigate,
                                                    PagingContext pagingContext,
                                                    PagingMetadata metadata,
                                                    IList<TResult> items,
                                                    bool isLastItem) {
        if (canNavigate == false || items.Count <= 0)
            return null;

        var targetItem = isLastItem ? items.Last() : items.First();
        var itemType = typeof(TResult);

        var sortValues = pagingContext.SortColumns.Select(s => {
            var dtoPropName = s.PropertyName.Equals(metadata.EntityKeyColumn, StringComparison.OrdinalIgnoreCase)
                ? metadata.DtoKeyColumn
                : s.PropertyName;

            var propInfo = GetCachedProperty(itemType, dtoPropName);
            var propVal = propInfo?.GetValue(targetItem);

            return (s.PropertyName, propVal);
        });

        object pkValue = GetPrimaryKeyValue(targetItem, metadata);
        var newCursor = new KeysetCursor(metadata.EntityKeyColumn, pkValue, sortValues);

        return CursorHelper.EncodeCursor(newCursor);
    }

    private object GetPrimaryKeyValue<TResult>(TResult item, PagingMetadata metadata) {
        var type = typeof(TResult);

        var propInfo = GetCachedProperty(type, metadata.DtoKeyColumn)
                    ?? GetCachedProperty(type, "Id")
                    ?? throw new InvalidOperationException($"Property '{metadata.DtoKeyColumn}' or 'Id' not found on type {type.Name}.");

        return propInfo.GetValue(item) ?? throw new InvalidOperationException($"Primary key value for property '{propInfo.Name}' on {type.Name} cannot be null.");
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}