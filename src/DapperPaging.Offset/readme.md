# DapperPaging.Offset

A high-performance, lightweight offset paging extension library for Dapper and SQL Server. Built on top of `Paging.Common`, `DapperPaging.Offset` parses complex filtering and sorting structures directly into parameterized SQL fragments and Dapper `DynamicParameters` to eliminate LINQ-to-SQL performance overhead.

## Features

* **Parameterized SQL Generation**: Safe SQL fragment generation avoiding SQL injection vulnerabilities.
* **Dynamic WHERE Builders**: Converts nested `FilterRequest` collections into structured `WHERE` clauses with full `AND`/`OR` support.
* **Column Mapping Support**: Maps incoming DTO property names to database columns dynamically using `propertyMap` dictionaries.
* **Single Round-Trip Execution**: Designed to work seamlessly with Dapper's `QueryMultipleAsync` to fetch record counts and page items in a single database round-trip.
* **Strict SQL Server Compatibility**: Generates standard `OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY` syntax.

## Installation

Install via the .NET CLI:

```bash
dotnet add package DapperPaging.Offset
```

Or via the Package Manager Console:

```bash
Install-Package DapperPaging.Offset
```

## Key Extension Methods

### 1. `BuildSqlWhere`

Generates a parameterized SQL `WHERE` clause string and populates a Dapper `DynamicParameters` collection.

```csharp
public static (string WhereClause, DynamicParameters Parameters) BuildSqlWhere(this PagedRequest request)
```

### 2. `BuildSqlSorting`

Generates a SQL `ORDER BY` clause string, falling back to a default sort when `SortBy` is not specified.

```csharp
public static string BuildSqlSorting(
    this PagedRequest request, 
    IDictionary<string, string>? propertyMap = null, 
    string defaultSortColumn = "Id")
```

## Usage Example

Below is an example of integrating `DapperPaging.Offset` in a FastEndpoints or API Controller handler using Dapper:

```csharp
using Dapper;
using DapperPaging.Offset.Data;
using DapperPaging.Offset.Filtering;
using Paging.Common;

public class SearchPeopleEndpoint : Endpoint<PeopleSearchRequest, PagedResponse<PersonDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SearchPeopleEndpoint(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public override async Task HandleAsync(PeopleSearchRequest req, CancellationToken ct)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // 1. Map DTO property names to Database column names if they differ
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        // 2. Generate SQL fragments and DynamicParameters
        var (whereClause, parameters) = req.BuildSqlWhere();
        var orderByClause = req.BuildSqlSorting(propertyMap, defaultSortColumn: "LastName");

        // 3. Add pagination parameters to the same DynamicParameters instance
        parameters.Add("Skip", req.Skip);
        parameters.Add("Take", req.PageSize);

        // 4. Combine Count and Data queries for single round-trip execution
        var sql = $@"
            SELECT COUNT(*) FROM People {whereClause};

            SELECT [PersonId] AS [Id], [FirstName], [LastName], [Email], [Age]
            FROM People
            {whereClause}
            {orderByClause}
            OFFSET @Skip ROWS
            FETCH NEXT @Take ROWS ONLY;
        ";

        // 5. Execute via QueryMultipleAsync
        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var totalCount = await multi.ReadFirstAsync<int>();
        var items = await multi.ReadAsync<PersonDto>();

        await Send.OkAsync(new PagedResponse<PersonDto>(items, totalCount, req.PageNumber, req.PageSize), ct);
    }
}
```

## Dependencies

* [Paging.Common](https://www.nuget.org/packages/Paging.Common)
* [Dapper](https://www.nuget.org/packages/Dapper)

## License

This project is licensed under the MIT License.