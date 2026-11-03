# Paging.Common

A lightweight, ORM-agnostic .NET library providing shared contracts, enums, models, and extensions for multi-condition filtering, dynamic sorting, and offset-based pagination. 

`Paging.Common` serves as the core foundation for data pagination across disparate data access layers—including Entity Framework Core (`IQueryable`) and Dapper (raw SQL generation)—without introducing heavyweight dependencies.

---

## Features

- **ORM-Agnostic Request & Response Models**: Unified `PagedRequest` and `PagedResponse<T>` data contracts.
- **Dynamic Multi-Condition Filtering**: Safe support for logical operators (`AND`/`OR`) and varied comparison modes (`MatchMode`).
- **Flexible Sorting**: Easy parsing and mapping of sorting rules (`SortBy` and `SortDirection`).
- **JSON Serialization Ready**: Built-in enum member converters for seamless integration with Web APIs and front-end data grids (e.g., PrimeNG, Ag-Grid).

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Paging.Common
```

Or via the Package Manager Console:

```powershell
Install-Package Paging.Common
```

---

## Core Models & Enums

### 1. `PagedRequest`
The primary payload structure sent from client applications or APIs.

```csharp
public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Skip => (PageNumber - 1) * PageSize;

    public string? SortBy { get; set; }
    public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

    public FilterOperator GlobalFilterOperator { get; set; } = FilterOperator.All;
    public List<FilterRequest>? Filters { get; set; }
}
```

### 2. `FilterRequest` & `ColumnFilter`
Structures supporting nested filter expressions with logical combinations.

```csharp
public class FilterRequest
{
    public FilterOperator Operator { get; set; } = FilterOperator.All; // AND or OR
    public List<ColumnFilter> Constraints { get; set; } = new();
}

public class ColumnFilter
{
    public string PropertyName { get; set; } = string.Empty;
    public MatchMode MatchMode { get; set; } = MatchMode.Equals;
    public object? Value { get; set; }
}
```

### 3. `MatchMode` Enums
Supported match modes include:
- `Equals`, `NotEquals`
- `Contains`, `NotContains`
- `StartsWith`, `EndsWith`
- `GreaterThan`, `GreaterThanOrEqualTo`
- `LessThan`, `LessThanOrEqualTo`

---

## Usage Examples

### Entity Framework Core (`IQueryable`)

Use `QueryableFilterExtensions` to apply pagination, sorting, and filtering directly to an `IQueryable<T>` set:

```csharp
using Paging.Common.Data;
using Paging.Common.Filtering;

public async Task<PagedResponse<PersonDto>> GetPeopleAsync(PagedRequest request, CancellationToken ct)
{
    IQueryable<Person> query = dbContext.People.AsNoTracking();

    // Applies filters and sorting dynamically
    var pagedResult = await query.ToPagedResponseAsync(request, ct);

    return pagedResult;
}
```

### Dapper / Raw SQL Generation

Consume the shared request parameters directly with custom SQL string builders:

```csharp
using Dapper;
using Paging.Common.Filtering;

public async Task<PagedResponse<PersonDto>> SearchPeopleAsync(PagedRequest request, CancellationToken ct)
{
    using var connection = dbConnectionFactory.CreateConnection();

    // Build WHERE clause and DynamicParameters using PagedRequest filters
    var (whereClause, parameters) = request.BuildSqlWhere();
    var orderByClause = request.BuildSqlSorting(propertyMap);

    parameters.Add("Skip", request.Skip);
    parameters.Add("Take", request.PageSize);

    var sql = $@"
        SELECT COUNT(*) FROM People {whereClause};

        SELECT [PersonId] AS [Id], [FirstName], [LastName], [Email]
        FROM People
        {whereClause}
        {orderByClause}
        OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
    ";

    using var multi = await connection.QueryMultipleAsync(sql, parameters);
    var totalCount = await multi.ReadFirstAsync<int>();
    var items = await multi.ReadAsync<PersonDto>();

    return new PagedResponse<PersonDto>(items, totalCount, request.PageNumber, request.PageSize);
}
```

---

## License

This project is licensed under the MIT License.