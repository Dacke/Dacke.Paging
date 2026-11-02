# EFCorePaging.Cursor

**Cursor-based (keyset) pagination for Entity Framework Core** using efficient index-based filtering for sequential navigation through large datasets.

## Features

- ✅ **Keyset Pagination**: Index-optimized cursor navigation (O(1) query performance)
- ✅ **Forward & Backward Navigation**: Seamless support for next/previous page traversal
- ✅ **Opaque Cursors**: Base64-encoded JSON tokens for secure, URL-safe pagination
- ✅ **Data Consistency**: Immune to drift from concurrent inserts/deletes
- ✅ **Advanced Filtering**: Extensible filtering framework integrated with cursor logic
- ✅ **Infinite Scroll Ready**: Perfect for mobile apps, feeds, and real-time updates

## Installation

```bash
dotnet add package EFCorePaging.Cursor
```

## Quick Start

### Basic Usage

```csharp
using EFCorePaging.Cursor;
using EFCorePaging.Cursor.Data;
using EFCorePaging.Cursor.Helpers;

// Initial request (no cursor)
var request = new PagedRequest 
{
    Cursor = null,                    // null for first page
    NavigationDirection = NavigationDirection.Forward,
    PageSize = 20,
    SortBy = "LastName"               // Primary sort column
};

// Decode cursor if provided from client
var cursor = CursorHelper.DecodeCursor<KeysetCursor>(request.Cursor);

// Build sort configuration
var sortColumns = new List<(string PropertyName, SortDirection Direction)>
{
    ("LastName", SortDirection.Ascending)
};

// Apply keyset paging
var query = dbContext.Users.AsQueryable();
var pagedQuery = query.ApplyKeysetPaging(
    cursor, 
    sortColumns, 
    primaryKeyName: "UserId",
    navDirection: request.NavigationDirection
);

// Fetch one extra record to detect if there's more data
var items = await pagedQuery.Take(request.PageSize + 1).ToListAsync();

// Check if there are more records beyond this page
bool hasMore = items.Count > request.PageSize;
var pageItems = items.Take(request.PageSize).ToList();

// Build next cursor from last item
KeysetCursor? nextCursor = null;
if (hasMore && pageItems.Count > 0)
{
    var lastItem = pageItems.Last();
    nextCursor = new KeysetCursor(
        primaryKeyName: "UserId",
        primaryKeyValue: lastItem.UserId,
        sortValues: new[] { ("LastName", (object?)lastItem.LastName) }
    );
}

// Encode cursor for client
string? nextCursorString = nextCursor != null 
    ? CursorHelper.EncodeCursor(nextCursor) 
    : null;

// Return response
var response = new PagedResponse<User>(
    values: pageItems,
    nextCursor: nextCursorString,
    previousCursor: request.Cursor,
    hasMore: hasMore,
    hasPrevious: cursor != null
);
```

### Multi-Column Sorting with Backward Navigation

```csharp
// Sort by multiple columns with mixed directions
var sortColumns = new List<(string PropertyName, SortDirection Direction)>
{
    ("Department", SortDirection.Ascending),
    ("LastName", SortDirection.Descending),
    ("FirstName", SortDirection.Ascending)
};

var query = dbContext.Users.AsQueryable();

// Navigate backward (get previous page)
var pagedQuery = query.ApplyKeysetPaging(
    cursor: previousCursor,
    sortColumns: sortColumns,
    primaryKeyName: "UserId",
    navDirection: NavigationDirection.Backward
);

var items = await pagedQuery.Take(request.PageSize + 1).ToListAsync();
```

### With Filtering

Combine cursor pagination with filtering (filtering handled by EFCorePaging.Common):

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

// Apply filters first, then pagination
var query = dbContext.Users
    .Where(/* filter conditions */);

var cursor = CursorHelper.DecodeCursor<KeysetCursor>(request.Cursor);
var sortColumns = new List<(string PropertyName, SortDirection Direction)>
{
    ("LastName", SortDirection.Ascending)
};

var pagedQuery = query.ApplyKeysetPaging(
    cursor, 
    sortColumns, 
    primaryKeyName: "UserId"
);

var items = await pagedQuery.Take(request.PageSize + 1).ToListAsync();
```

## API Reference

### PagedRequest

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

### QueryableKeysetExtensions

Extension methods for `IQueryable<T>`.

#### ApplyKeysetPaging\<T\>

```csharp
public static IQueryable<T> ApplyKeysetPaging<T>(
    this IQueryable<T> query, 
    KeysetCursor? cursor, 
    List<(string PropertyName, SortDirection Direction)> sortColumns, 
    string primaryKeyName, 
    NavigationDirection navDirection = NavigationDirection.Forward)
```

Applies keyset-based pagination filtering to a queryable.

**Parameters:**
- `query`: The queryable to paginate
- `cursor`: The cursor from the previous page (null for first page)
- `sortColumns`: List of sort columns with directions (order matters)
- `primaryKeyName`: Name of the primary key column for uniqueness
- `navDirection`: Direction to navigate (Forward or Backward)

**Returns:** Filtered `IQueryable<T>` with keyset predicate applied

**Remarks:**
- Primary key is automatically appended to sort columns if not already present
- Always fetch `PageSize + 1` records to detect if more data exists
- Backward navigation automatically inverts sort directions for reverse seeking

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
// Fetch one extra to detect if more data exists
var items = await query.Take(pageSize + 1).ToListAsync();
var hasMore = items.Count > pageSize;
var pageItems = items.Take(pageSize);
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

- **[EFCorePaging.Common](https://www.nuget.org/packages/EFCorePaging.Common/)** - Shared filtering, enums, and types
- **[EFCorePaging.Offset](https://www.nuget.org/packages/EFCorePaging.Offset/)** - Offset-based pagination for traditional page navigation

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Repository

[github.com/Dacke/EFCorePaging](https://github.com/Dacke/EFCorePaging)
