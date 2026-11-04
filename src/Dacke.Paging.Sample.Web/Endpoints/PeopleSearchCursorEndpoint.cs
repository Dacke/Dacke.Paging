using Dacke.EFCorePaging.Cursor;
using Dacke.EFCorePaging.Cursor.Builders;
using Dacke.EFCorePaging.Cursor.Data;
using Dacke.EFCorePaging.Cursor.Extensions;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PeopleSearchCursorEndpoint : Endpoint<PagedRequest, PagedResponse<PersonDto>> {
    private readonly ITestDataContextFactory dbContextFactory;
    private readonly ICursorPagedResponseBuilder pagedResponseBuilder;

    public PeopleSearchCursorEndpoint(ITestDataContextFactory dbContextFactory, ICursorPagedResponseBuilder pagedResponseBuilder) {
        this.dbContextFactory = dbContextFactory;
        this.pagedResponseBuilder = pagedResponseBuilder;
    }

    public override void Configure() => Post("people/search/cursor");

    public override async Task HandleAsync(PagedRequest req, CancellationToken ct) {
        using var db = dbContextFactory.CreateDbContext();

        var metadata = new PagingMetadata(nameof(Person.PersonId),
                                          nameof(Person.PersonId),
                                          nameof(PersonDto.Id));
                
        var pagingContext = req.BuildPagingContext(metadata);

        var results = await db.People
            .AsNoTracking()
            .ApplyKeysetPaging(pagingContext)
            .Take(pagingContext.PageSize + 1)
            .ProjectToType<PersonDto>()
            .ToListAsync(ct);

        var response = pagedResponseBuilder.BuildResponse(results, pagingContext, metadata);

        await Send.OkAsync(response, ct);
    }
}