using EFCorePaging.Cursor;
using EFCorePaging.Cursor.Data;
using EFCorePaging.Cursor.Enums;
using EFCorePaging.Cursor.Helpers;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Paging.Common.Enums;
using Paging.Common.Sorting;
using Paging.Sample.Web.Data;
using Paging.Sample.Web.Data.Models;
using Paging.Sample.Web.Dtos;
using System.Collections.Concurrent;
using System.Reflection;

public class PeopleSearchCursorEndpoint : Endpoint<PagedRequest, PagedResponse<PersonDto>> {
    private readonly ITestDataContextFactory dbContextFactory;

    private PagingMetadata PagingMetadata => new(nameof(Person.PersonId),
                                                 nameof(Person.PersonId),
                                                 nameof(PersonDto.Id));

    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    public PeopleSearchCursorEndpoint(ITestDataContextFactory dbContextFactory) => this.dbContextFactory = dbContextFactory;
        
    public override void Configure() => Post("people/search/cursor");

    public override async Task HandleAsync(PagedRequest req, CancellationToken ct) {
        using var db = dbContextFactory.CreateDbContext();

        ArgumentException.ThrowIfNullOrWhiteSpace(PagingMetadata.DefaultSortColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(PagingMetadata.EntityKeyColumn);

        int pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize;
        var cursor = CursorHelper.DecodeCursor<KeysetCursor>(req.Cursor);
        var sortColumns = SortParser.ParseSortColumns(req.SortBy, req.DefaultSortDirection, defaultColumn: PagingMetadata.DefaultSortColumn);

        var pagingContext = new PagingContext(pageSize, cursor, sortColumns, PagingMetadata.EntityKeyColumn, req.NavigationDirection);

        var results = await db.People
            .AsNoTracking()
            .ApplyKeysetPaging(pagingContext.Cursor, pagingContext.SortColumns, pagingContext.PrimaryKeyColumn, pagingContext.NavigationDirection)
            .Take(pagingContext.PageSize + 1)
            .ProjectToType<PersonDto>()
            .ToListAsync(ct);

        bool isBackward = req.NavigationDirection == NavigationDirection.Backward;
        bool hasBoundaryMatch = results.Count > pageSize;

        if (hasBoundaryMatch)
            results.RemoveAt(results.Count - 1);

        if (isBackward)
            results = [.. Enumerable.Reverse(results)];

        bool hasMore = isBackward ? (cursor != null) : hasBoundaryMatch;
        bool hasPrevious = isBackward ? hasBoundaryMatch : (cursor != null);

        string? nextCursor = BuildCursorForBoundary(hasMore, sortColumns, results, isLastItem: true);
        string? previousCursor = BuildCursorForBoundary(hasPrevious, sortColumns, results, isLastItem: false);
        
        var response = new PagedResponse<PersonDto>(results, nextCursor, previousCursor, hasMore, hasPrevious);

        await Send.OkAsync(response, ct);
    }


    private object GetPrimaryKeyValue(PersonDto item) {
        var type = typeof(PersonDto);

        var propInfo = GetCachedProperty(type, PagingMetadata.DtoKeyColumn)
                    ?? GetCachedProperty(type, "Id")
                    ?? throw new InvalidOperationException($"Property '{PagingMetadata.DtoKeyColumn}' or 'Id' not found on type {type.Name}.");

        return propInfo.GetValue(item) ?? throw new InvalidOperationException($"Primary key value for property '{propInfo.Name}' on {type.Name} cannot be null.");
    }

    private string? BuildCursorForBoundary(bool canNavigate, List<(string PropertyName, SortDirection Direction)> sortColumns, IList<PersonDto> items, bool isLastItem) {

        if (canNavigate == false || items.Count <= 0)
            return null;

        var targetItem = isLastItem ? items.Last() : items.First();
        var itemType = typeof(PersonDto);

        var sortValues = sortColumns.Select(s => {
            var dtoPropName = s.PropertyName.Equals(PagingMetadata.EntityKeyColumn, StringComparison.OrdinalIgnoreCase)
                ? PagingMetadata.DtoKeyColumn
                : s.PropertyName;

            var propInfo = GetCachedProperty(itemType, dtoPropName);
            var propVal = propInfo?.GetValue(targetItem);

            return (s.PropertyName, propVal);
        });

        object pkValue = GetPrimaryKeyValue(targetItem);
        var newCursor = new KeysetCursor(PagingMetadata.EntityKeyColumn, pkValue, sortValues);

        return CursorHelper.EncodeCursor(newCursor);
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}