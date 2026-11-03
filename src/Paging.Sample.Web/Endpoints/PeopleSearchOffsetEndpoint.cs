using EFCorePaging.Offset;
using EFCorePaging.Offset.Data;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Paging.Common.Data;
using Paging.Sample.Web.Data;
using Paging.Sample.Web.Data.Models;
using Paging.Sample.Web.Dtos;

public class PeopleSearchOffsetEndpoint : Endpoint<PeopleSearchRequest, PagedResponse<PersonDto>> {
    private readonly ITestDataContextFactory dbContextFactory;

    public PeopleSearchOffsetEndpoint(ITestDataContextFactory dbContextFactory) => this.dbContextFactory = dbContextFactory;
        
    public override void Configure() => Post("people/search/offset");

    public override async Task HandleAsync(PeopleSearchRequest req, CancellationToken ct) {
        using var db = dbContextFactory.CreateDbContext();

        var baseQuery = db.People
                          .AsNoTracking()
                          .ApplyFilters(req.Filters, req.GlobalFilterOperator);
                
        var totalCount = await baseQuery.CountAsync(ct);

        // Define DTO -> Entity property mapping (e.g. DTO "Id" maps to Entity "PersonId")
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        // Apply sorting and offset paging
        var personDtos = await baseQuery
                         .ApplySorting(req.SortBy, req.DefaultSortDirection, propertyMap)
                         .Skip(req.Skip)
                         .Take(req.PageSize)
                         .ProjectToType<PersonDto>()
                         .ToListAsync(ct);
        
        var response = new PagedResponse<PersonDto>(personDtos, totalCount);

        await Send.OkAsync(response, ct);
    }
}

public class PeopleSearchRequest : PagedRequest {
    public override string SortBy { get; set; } = $"{nameof(PersonDto.LastName)}, {nameof(PersonDto.FirstName)}";
}