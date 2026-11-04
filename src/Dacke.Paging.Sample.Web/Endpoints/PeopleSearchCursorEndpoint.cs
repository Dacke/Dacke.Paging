using Dacke.EFCorePaging.Cursor;
using Dacke.EFCorePaging.Cursor.Data;
using Dacke.EFCorePaging.Cursor.Enums;
using Dacke.EFCorePaging.Cursor.Extensions;
using Dacke.EFCorePaging.Cursor.Helpers;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Sorting;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq.Expressions;
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
                
        var pagingContext = req.BuildPagingContext(PagingMetadata);

        var results = await db.People
            .AsNoTracking()
            .ApplyKeysetPaging(pagingContext)
            .Take(pagingContext.PageSize + 1)
            .ProjectToType<PersonDto>()
            .ToListAsync(ct);

        bool isBackward = req.NavigationDirection == NavigationDirection.Backward;
        bool hasBoundaryMatch = results.Count > req.PageSize;

        if (hasBoundaryMatch)
            results.RemoveAt(results.Count - 1);

        if (isBackward)
            results = [.. Enumerable.Reverse(results)];

        bool hasMore = isBackward ? (pagingContext.Cursor != null) : hasBoundaryMatch;
        bool hasPrevious = isBackward ? hasBoundaryMatch : (pagingContext.Cursor != null);

        string? nextCursor = BuildCursorForBoundary(hasMore, pagingContext.SortColumns, results, isLastItem: true);
        string? previousCursor = BuildCursorForBoundary(hasPrevious, pagingContext.SortColumns, results, isLastItem: false);
        
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