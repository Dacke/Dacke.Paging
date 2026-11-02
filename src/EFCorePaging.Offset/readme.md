# EFCorePaging.Offset

**Offset-based pagination for Entity Framework Core** using efficient `Skip`/`Take` queries optimized for modern data grid components.

## Features

- ✅ **Offset Pagination**: Standard `Skip`/`Take` pattern for positional page navigation
- ✅ **Flexible Sorting**: Multi-field sorting with ascending/descending support
- ✅ **Advanced Filtering**: Extensible filtering framework with multiple operators
- ✅ **Performance Optimized**: Lightweight queries designed to maintain efficient database execution plans
- ✅ **UI Component Compatible**: Works seamlessly with PrimeNG, Kendo UI, and other modern data grids

## Installation

```bash
dotnet add package EFCorePaging.Offset
```

## Quick Start

### Basic Usage

```csharp
using EFCorePaging.Offset;
using EFCorePaging.Offset.Data;

// Create a paged request
var request = new PagedRequest 
{
    Skip = 0,           // Number of records to skip
    PageSize = 20,      // Records per page
    SortBy = "LastName" // Sort by property name
};

// Apply to your queryable
var query = dbContext.Users.AsQueryable();
var sortedQuery = query.ApplySorting(request.SortBy);

// Skip and take for pagination
var pagedQuery = sortedQuery.Skip(request.Skip).Take(request.PageSize);

// Execute and get results
var items = await pagedQuery.ToListAsync();
var totalCount = await query.CountAsync();

// Return paginated response
var response = new PagedResponse<User>(items, totalCount);
```

### Advanced Sorting

Sort by multiple fields with mixed directions:

```csharp
var request = new PagedRequest 
{
    SortBy = "Department asc, LastName desc, FirstName asc",
    Skip = 0,
    PageSize = 20
};

var sorted = dbContext.Users.ApplySorting(request.SortBy);
```

### With Filtering

```csharp
var request = new PagedRequest 
{
    Skip = 0,
    PageSize = 20,
    SortBy = "LastName",
    Filters = new List<FilterRequest>
    {
        new FilterRequest 
        { 
            Field = "Department", 
            Operator = "equals", 
            Value = "Sales" 
        }
    },
    GlobalFilterOperator = FilterOperator.All
};

var query = dbContext.Users.AsQueryable();
// Apply filters (filtering logic handled by EFCorePaging.Common)
var filtered = query.ApplySorting(request.SortBy);
var paged = filtered.Skip(request.Skip).Take(request.PageSize);
```

## API Reference

### PagedRequest

Configuration object for pagination requests.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Skip` | int | 0 | Number of records to skip |
| `PageSize` | int | 20 | Records per page |
| `SortBy` | string | "" | Comma-separated sort fields (supports `asc`/`desc` modifiers) |
| `DefaultSortDirection` | SortDirection | Ascending | Default sort direction when not specified |
| `GlobalFilterOperator` | FilterOperator | All | How to combine multiple filters |
| `Filters` | List\<FilterRequest\> | [] | Collection of filters to apply |

### PagedResponse\<T\>

Result container for paginated data.

| Property | Type | Description |
|----------|------|-------------|
| `Values` | T[] | The records returned for the current page |
| `TotalCount` | int | Total number of records matching the filter criteria |

### QueryableOffsetExtensions

Extension methods for `IQueryable<T>`.

#### ApplySorting\<T\>

```csharp
public static IQueryable<T> ApplySorting<T>(
    this IQueryable<T> query, 
    string sortBy, 
    SortDirection defaultDirection = SortDirection.Ascending, 
    Dictionary<string, string>? propertyMap = null)
```

Applies dynamic sorting to a queryable.

**Parameters:**
- `query`: The queryable to sort
- `sortBy`: Comma-separated property names with optional `asc`/`desc` modifiers (e.g., "LastName desc, FirstName asc")
- `defaultDirection`: Default sort direction for fields without explicit modifiers
- `propertyMap`: Optional mapping from DTO property names to Entity property names

**Returns:** Sorted `IQueryable<T>`

**Throws:**
- `ArgumentException`: If a property in `sortBy` is not found on type T

## Performance Considerations

- **Offset pagination** is best suited for smaller result sets or datasets with indexed sort columns
- For very large datasets (1M+ records), consider **cursor-based pagination** for position-based navigation
- Ensure your sort columns are properly indexed in the database
- Use projection to select only needed columns before `Skip` and `Take` operations

## Property Mapping

For DTO-to-Entity property mapping, provide a mapping dictionary:

```csharp
var propertyMap = new Dictionary<string, string>
{
    { "FullName", "LastName" },
    { "Department", "Dept" }
};

var sorted = query.ApplySorting(request.SortBy, propertyMap: propertyMap);
```

## Related Packages

- **[EFCorePaging.Common](https://www.nuget.org/packages/EFCorePaging.Common/)** - Shared filtering, enums, and types
- **[EFCorePaging.Cursor](https://www.nuget.org/packages/EFCorePaging.Cursor/)** - Cursor-based pagination for sequential navigation

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Repository

[github.com/Dacke/EFCorePaging](https://github.com/Dacke/EFCorePaging)
