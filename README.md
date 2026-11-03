# Entity Framework Core, Dapper & FastEndpoints Pagination

This repository contains different database pagination strategies within a .NET Web API built using **FastEndpoints**, **Mapster**, **Entity Framework Core**, and **Dapper**. 

When building RESTful APIs that query database tables with large datasets, choosing the right pagination technique is critical for balancing query performance, server resource usage, and front-end UI compatibility.

---

## Pagination Strategies Comparison

This project compares two primary methods for server-side pagination: **Offset-Based Pagination (`Skip` / `Take`)** and **Cursor-Based Pagination (Keyset)**.

### 1. Offset-Based Pagination (`Skip` / `Take`)

Offset pagination relies on numerical page offsets. The client requests a specific window of data by sending the number of records to skip (`Skip` / `First`) and the number of records to fetch (`Take` / `PageSize`).

#### EF Core Pattern
```csharp
var items = await db.People
    .Where(filterPredicate)
    .OrderBy(p => p.LastName)
    .ThenBy(p => p.PersonId)
    .Skip(req.Skip)      // e.g., Skip 40
    .Take(req.PageSize)  // e.g., Take 20
    .ToListAsync();
```

#### Pros
* **Random Access:** Allows clients to jump directly to any arbitrary page number (e.g., jump from Page 1 straight to Page 42).
* **UI Controls Support:** Seamlessly provides metadata required for traditional pagination UI components (e.g., `TotalCount`, `TotalPages`, current page indicators).
* **Simplified Multi-Column Sorting:** Standard `OrderBy` and `ThenBy` expression chains map directly to standard SQL clauses without extra predicate logic.

#### Cons
* **Performance Degradation at Depth:** Database engines must scan and discard `N` rows before returning the requested page (`OFFSET N ROWS`). As page depth increases, query latency grows linearly or exponentially.
* **Data Drift (Inconsistency):** If records are inserted or deleted while a user is navigating pages, items may be skipped or shown twice across consecutive pages.

---

### 2. Cursor-Based / Keyset Pagination

Keyset pagination uses the values of the last retrieved row (a "cursor", often encoded as Base64 JSON) to anchor the start of the next page. It translates pagination into standard `WHERE` clause filtering rather than database skipping.

#### EF Core Pattern
```csharp
// Fetch next page where key > last_seen_key
var items = await db.People
    .Where(filterPredicate)
    .Where(p => p.LastName > lastLastName || (p.LastName == lastLastName && p.PersonId > lastPersonId))
    .OrderBy(p => p.LastName)
    .ThenBy(p => p.PersonId)
    .Take(req.PageSize)
    .ToListAsync();
```

#### Pros
* **Constant Time Performance ($O(1)$):** Leverages database indexes directly (`WHERE Id > @lastId`). Query performance remains virtually identical whether fetching the first page or the millionth page.
* **Data Consistency:** Immune to data drift caused by concurrent insertions or deletions while a user iterates through records.
* **Ideal for Infinite Scroll:** Perfect for mobile apps or feeds where users continuously scroll downward.

#### Cons
* **No Random Page Access:** Users cannot jump to an arbitrary page number (e.g., page 10) because the cursor value for page 10 is unknown until page 9 is retrieved.
* **Complex Multi-Column Logic:** Constructing dynamic composite keyset predicates (`(ColA > ValA) OR (ColA == ValA AND ColB > ValB)`) across arbitrary sort columns requires complex expression tree building.
* **No Total Row Counts:** Obtaining total page counts requires executing an expensive secondary `COUNT(*)` query, nullifying the efficiency gains of cursor queries.

---

## Architectural Decision Summary

### Selected Strategy: **Offset-Based (`Skip` / `Take`) Pagination**

For this project, **Offset-Based (`Skip` / `Take`) Pagination** was selected as the standard implementation.

#### Justification
Modern front-end component suites such as **PrimeNG** (Angular) and **Kendo UI** rely on lazy-loading table structures that send explicit positional offset state (e.g., `first: 40, rows: 20`, or `skip: 40, take: 20`). These UI controls explicitly allow end-users to:
1. Jump directly to arbitrary page numbers.
2. Change page sizes dynamically from dropdown selectors.
3. Display total record counts and page totals (`1-20 of 1,420 items`).

Because cursor-based pagination prohibits random-access page jumps and requires sequential cursor propagation, it is incompatible with standard PrimeNG and Kendo UI data grid contracts. 

To mitigate performance issues associated with standard offset paging on deeper datasets, this API utilizes lightweight index queries (`ApplyOffsetSorting` extensions) alongside projection (`ProjectToType<T>()`) to maintain efficient database execution plans.
