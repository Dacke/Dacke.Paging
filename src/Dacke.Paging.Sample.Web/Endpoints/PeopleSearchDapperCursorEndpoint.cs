using Dacke.DapperPaging.Cursor;
using Dacke.DapperPaging.Cursor.Filtering;
using Dacke.DapperPaging.Cursor.Sorting;
using Dacke.DapperPaging.Offset.Sorting;
using Dacke.Paging.Common.Builders;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Extensions;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using Dapper;
using FastEndpoints;

public class PeopleSearchDapperCursorEndpoint : Endpoint<PagedRequest, PagedResponse<PersonDto>> {
    private readonly IDbConnectionFactory dbConnectionFactory;
    private readonly ICursorPagedResponseBuilder pagedResponseBuilder;

    public PeopleSearchDapperCursorEndpoint(IDbConnectionFactory dbConnectionFactory, ICursorPagedResponseBuilder pagedResponseBuilder) {
        this.dbConnectionFactory = dbConnectionFactory;
        this.pagedResponseBuilder = pagedResponseBuilder;
    }

    public override void Configure() => Post("people/search/dapper/cursor");

    public override async Task HandleAsync(PagedRequest req, CancellationToken ct) {
        using var connection = dbConnectionFactory.CreateConnection();

        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        var metadata = new PagingMetadata(nameof(Person.PersonId),
                                          nameof(Person.PersonId),
                                          nameof(PersonDto.Id));

        var pagingContext = req.BuildPagingContext(metadata);

        // Single method call handles filters, cursors, and parameter merging
        var (whereClause, parameters) = req.BuildSqlWhere(pagingContext, propertyMap);
        var orderByClause = req.BuildCursorSqlSorting(pagingContext, metadata, propertyMap);

        // Request PageSize + 1 for boundary match detection in the builder
        parameters.Add("Take", req.PageSize + 1);

        var sql = $@"
        SELECT [PersonId] AS [Id], [FirstName], [LastName], [Email], [PhoneNumber], [Address], [City], [State], [PostalCode], [Age], [BirthDay]
        FROM People
        {whereClause}
        {orderByClause}
        OFFSET 0 ROWS
        FETCH NEXT @Take ROWS ONLY";

        Console.WriteLine(sql); //  Only for debugging

        var people = await connection.QueryAsync<PersonDto>(sql, parameters);
        
        var response = pagedResponseBuilder.BuildResponse<PagedResponse<PersonDto>, PersonDto>([.. people], pagingContext, metadata);

        await Send.OkAsync(response, ct);
    }
}