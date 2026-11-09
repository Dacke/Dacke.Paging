using Dacke.EFCorePaging.Cursor;
using Dacke.EFCorePaging.Cursor.Data;
using Dacke.Paging.Common.Builders;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Data;
using Dacke.Paging.Common.Extensions;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PeopleSearchEfCoreCursorEndpoint : Endpoint<PagedRequest, PagedResponse<PersonDto>> {
    private readonly ITestDataContextFactory dbContextFactory;
    private readonly ICursorPagedResponseBuilder pagedResponseBuilder;

    public PeopleSearchEfCoreCursorEndpoint(ITestDataContextFactory dbContextFactory, ICursorPagedResponseBuilder pagedResponseBuilder) {
        this.dbContextFactory = dbContextFactory;
        this.pagedResponseBuilder = pagedResponseBuilder;
    }

    public override void Configure() => Post("people/search/efcore/cursor");

    public override async Task HandleAsync(PagedRequest req, CancellationToken ct) {
        using var db = dbContextFactory.CreateDbContext();

        var metadata = new PagingMetadata(nameof(Person.PersonId),
                                          nameof(Person.PersonId),
                                          nameof(PersonDto.Id));
        
        var pagingContext = req.BuildPagingContext(metadata);

        var results = await db.People
            .AsNoTracking()
            .ApplyFilters(req.Filters, req.GlobalFilterOperator)
            .ApplyKeysetPaging(pagingContext)
            .Take(pagingContext.PageSize + 1)
            .ProjectToType<PersonDto>()
            .ToListAsync(ct);
                
        var response = pagedResponseBuilder.BuildResponse<PagedResponse<PersonDto>, PersonDto>(results, pagingContext, metadata);

        await Send.OkAsync(response, ct);
    }
}