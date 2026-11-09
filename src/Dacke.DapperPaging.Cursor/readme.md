# Dacke.DapperPaging.Cursor

**Cursor-based (keyset) pagination for Dapper** using efficient index-based filtering for sequential navigation through large datasets via raw SQL or micro-ORM builders.

## Features

- ✅ **Keyset Pagination**: Index-optimized cursor navigation (O(1) query performance)
- ✅ **Forward & Backward Navigation**: Seamless support for next/previous page traversal
- ✅ **Opaque Cursors**: Base64-encoded JSON tokens for secure, URL-safe pagination
- ✅ **Data Consistency**: Immune to drift from concurrent inserts/deletes
- ✅ **Advanced Filtering**: Extensible filtering framework integrated with cursor logic
- ✅ **Infinite Scroll Ready**: Perfect for mobile apps, feeds, and real-time updates

## Installation

```bash
dotnet add package Dacke.DapperPaging.Cursor
```

## Quick Start

### Basic Usage

```csharp
// Initial request (no cursor)
var request = new PagedCursorRequest {
    Cursor = null,                    // null for first page
    NavigationDirection = NavigationDirection.Forward,
    PageSize = 20,
    SortBy = "LastName"               // Primary sort column
};

public class PeopleSearchCursorEndpoint : Endpoint<PagedCursorRequest, PagedResponse<PersonDto>> {
    private readonly IDbConnectionFactory dbConnectionFactory;
    private readonly ICursorPagedResponseBuilder pagedResponseBuilder;

    public PeopleSearchCursorEndpoint(IDbConnectionFactory dbConnectionFactory, ICursorPagedResponseBuilder pagedResponseBuilder) {
        this.dbConnectionFactory = dbConnectionFactory;
        this.pagedResponseBuilder = pagedResponseBuilder;
    }

    public override void Configure() => Post("people/search/cursor");

    public override async Task HandleAsync(PagedCursorRequest req, CancellationToken ct) {
        using var connection = dbConnectionFactory.CreateConnection();

        var metadata = new PagingMetadata(nameof(Person.PersonId),
                                          nameof(Person.PersonId),
                                          nameof(PersonDto.Id));
                
        var pagingContext = req.BuildPagingContext(metadata);

        // Generate SQL query and Dapper parameters using the keyset builder
        var (sql, parameters) = SqlKeysetBuilder.BuildQuery<PersonDto>(
            baseSql: "SELECT PersonId AS Id, FirstName, LastName, Department FROM People",
            pagingContext: pagingContext
        );

        // Fetch PageSize + 1 records using Dapper
        var entities = await connection.QueryAsync<PersonDto>(sql, parameters);
        var results = entities.ToList();

        var response = pagedResponseBuilder.BuildResponse(results, pagingContext, metadata);

        await Send.OkAsync(response, ct);
    }
}
```

### Multi-Column Sorting with Backward Navigation

```csharp
// Sort by multiple columns with mixed directions
var request = new PagedCursorRequest {
    Cursor = null,                    // null for first page
    NavigationDirection = NavigationDirection.Forward,
    PageSize = 20,
    SortBy = "LastName desc, FirstName asc"
};

//  No Change to the endpoint
```

### With Filtering

Combine cursor pagination with filtering (filtering handled by Paging.Common):

```csharp
var request = new PagedRequest 
{
    Cursor = null,
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

//  No Change to the endpoint
```

## API Reference

### PagedCursorRequest

Configuration object for cursor-based pagination requests.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Cursor` | string? | null | Opaque Base64-encoded cursor string from previous response |
| `NavigationDirection` | NavigationDirection | Forward | Direction of pagination (Forward or Backward) |
| `PageSize` | int | 20 | Records per page |
| `SortBy` | string | "" | Primary sort column name |
| `DefaultSortDirection` | SortDirection | Ascending | Sort direction (Ascending or Descending) |
| `GlobalFilterOperator` | FilterOperator | All | How to combine multiple filters |
| `Filters` | List\<FilterRequest\> | [] | Collection of filters to apply |

### PagedResponse\<T\>

Result container for paginated cursor-based data.

| Property | Type | Description |
|----------|------|-------------|
| `Values` | T[] | Records returned for the current page |
| `HasMore` | bool | Whether additional records exist beyond this page |
| `NextCursor` | string? | Opaque cursor for fetching the next page |
| `HasPrevious` | bool | Whether a previous page exists |
| `PreviousCursor` | string? | Opaque cursor for fetching the previous page |

### KeysetCursor

Cursor payload containing the keyset anchor point.

| Property | Type | Description |
|----------|------|-------------|
| `PrimaryKeyName` | string | Name of the primary key column (e.g., "UserId") |
| `PrimaryKeyValue` | string | String representation of the primary key value |
| `SortValues` | Dictionary\<string, string?\> | Map of sort column names to their string values |

### NavigationDirection

Enum controlling pagination direction.

```csharp
public enum NavigationDirection
{
    Forward = 0,    // Fetch next page
    Backward = 1    // Fetch previous page
}
```

### SqlKeysetBuilder

Utility for translating paging context into SQL statements and Dapper parameters.

#### BuildQuery\<T\>

```csharp
public static (string Sql, DynamicParameters Parameters) BuildQuery<T>(
    string baseSql,
    PagingContext pagingContext)
```

Appends dynamic keyset WHERE clauses, sorting (`ORDER BY`), and limit constraints (`TOP` / `LIMIT`) to a base SQL string.

**Parameters:**
- `baseSql`: The underlying SQL query (e.g., `SELECT * FROM Users`)
- `pagingContext`: The active paging context generated from the request

**Returns:** A tuple containing the fully parameterized SQL string and Dapper `DynamicParameters`

**Remarks:**
- Automatically manages primary key tie-breakers for deterministic sorting
- Always fetches `PageSize + 1` records to detect if more data exists
- Adapts directional comparison operators (`>` vs `<`) depending on sort direction and forward/backward navigation

### CursorHelper

Utility class for cursor encoding/decoding.

#### EncodeCursor\<T\>

```csharp
public static string EncodeCursor<T>(T cursorData)
```

Serializes and Base64-encodes a cursor object into an opaque string suitable for URLs.

**Parameters:**
- `cursorData`: The KeysetCursor or custom cursor object to encode

**Returns:** Base64-encoded cursor string

#### DecodeCursor\<T\>

```csharp
public static T? DecodeCursor<T>(string? cursor)
```

Decodes a Base64 cursor string back into a strongly-typed object.

**Parameters:**
- `cursor`: The encoded cursor string from a previous response

**Returns:** Decoded cursor object, or null/default if invalid

**Remarks:**
- Safely handles null or empty cursor strings
- Returns default if Base64 or JSON decoding fails

## Performance Characteristics

### Advantages
- **O(1) Complexity**: Query performance is constant regardless of dataset size
- **Index-Friendly**: Leverages database indexes efficiently with WHERE predicates
- **Data Consistency**: Unaffected by concurrent inserts/deletes during pagination
- **Scalable**: Perfect for millions of rows or infinite scroll scenarios

### Limitations
- **No Random Access**: Cannot jump to arbitrary page numbers (e.g., "page 42")
- **Sequential Navigation**: Users must traverse pages sequentially using cursors
- **No Total Count**: Cannot display "X of 1,000,000 items" without expensive COUNT query

## Best Practices

### 1. Always Fetch PageSize + 1
```csharp
// Fetch one extra to detect if more data exists (handled automatically by SqlKeysetBuilder)
var entities = await connection.QueryAsync<User>(sql, parameters);
var items = entities.ToList();
var hasMore = items.Count > pageSize;
```

### 2. Index Your Sort Columns
Ensure database indexes exist on all sort columns for optimal performance:
```sql
CREATE INDEX idx_users_lastname_userid 
ON Users(LastName ASC, UserId ASC);
```

### 3. Include Primary Key in Sort
Always include the primary key as the final sort column to ensure consistent ordering:
```csharp
var sortColumns = new List<(string, SortDirection)>
{
    ("Department", SortDirection.Ascending),
    ("LastName", SortDirection.Ascending),
    ("UserId", SortDirection.Ascending)  // Primary key
};
```

### 4. Handle Cursor Validation
```csharp
var cursor = CursorHelper.DecodeCursor<KeysetCursor>(request.Cursor);
if (cursor != null && cursor.SortValues.Count == 0)
{
    // Invalid or corrupted cursor, reset to first page
    cursor = null;
}
```

### 5. Return Previous Cursor as-is
Pass the current request's cursor as the previous cursor in the response:
```csharp
var response = new PagedResponse<User>(
    values: items,
    nextCursor: nextCursorEncoded,
    previousCursor: request.Cursor,  // Echo back the request cursor
    hasMore: hasMore,
    hasPrevious: request.Cursor != null
);
```

## Comparison: Cursor vs. Offset Pagination

| Feature | Cursor-Based | Offset-Based |
|---------|-------------|--------------|
| **Query Performance** | O(1) constant | O(n) linear degradation |
| **Random Page Access** | ❌ No | ✅ Yes |
| **Data Consistency** | ✅ Yes | ❌ No (data drift) |
| **Total Record Count** | ❌ Expensive | ✅ Easy |
| **Infinite Scroll** | ✅ Perfect | ⚠️ Okay |
| **Backward Navigation** | ✅ Yes | ✅ Yes |
| **URL Friendliness** | ⚠️ Opaque tokens | ✅ Simple parameters |

## Use Cases

**Choose Cursor Pagination for:**
- Very large datasets (10M+ records)
- Infinite scroll / feed-style UIs
- Mobile applications
- Real-time data with frequent updates
- API rate-limited by query cost

**Choose Offset Pagination for:**
- Traditional paginated tables with page numbers
- Requirements to jump to specific pages
- Display of total record counts
- Smaller datasets or well-indexed columns

## Related Packages

- **[Dacke.Paging.Common](https://www.nuget.org/packages/Dacke.Paging.Common/)** - Shared filtering, enums, and types
- **[Dacke.Paging.Offset](https://www.nuget.org/packages/Dacke.Paging.Offset/)** - Offset-based pagination for traditional page navigation

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Repository

[github.com/Dacke/Dacke.Paging](https://github.com/Dacke/Dacke.Paging)