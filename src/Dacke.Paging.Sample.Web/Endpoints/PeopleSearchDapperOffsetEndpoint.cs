using Dacke.DapperPaging.Offset;
using Dacke.DapperPaging.Offset.Data;
using Dacke.DapperPaging.Offset.Filtering;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using Dapper;
using FastEndpoints;

namespace Dacke.Paging.Sample.Web.Endpoints;

public class PeopleSearchDapperOffsetEndpoint : Endpoint<PagedRequest, PagedResponse<PersonDto>> {
    private readonly IDbConnectionFactory dbConnectionFactory;

    public PeopleSearchDapperOffsetEndpoint(IDbConnectionFactory dbConnectionFactory) => this.dbConnectionFactory = dbConnectionFactory;

    public override void Configure() => Post("people/search/dapper");

    public override async Task HandleAsync(PagedRequest req, CancellationToken ct) {
        using var connection = dbConnectionFactory.CreateConnection();

        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { nameof(PersonDto.Id), nameof(Person.PersonId) }
        };

        var (whereClause, parameters) = req.BuildSqlWhere();
        var orderByClause = req.BuildSqlSorting(propertyMap);

        // Add pagination parameters
        parameters.Add("Skip", req.Skip);
        parameters.Add("Take", req.PageSize);

        // Combine both queries into a single batch separated by a semicolon
        var sql = $@"
        SELECT COUNT(*) FROM People {whereClause};

        SELECT [PersonId] AS [Id], [FirstName], [LastName], [Email], [PhoneNumber], [Address], [City], [State], [PostalCode], [Age], [BirthDay]
        FROM People
        {whereClause}
        {orderByClause}
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY;";

        Console.WriteLine(sql); //  Only for debugging

        // Execute both queries in a single round-trip
        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var personCount = await multi.ReadFirstAsync<int>();
        var people = await multi.ReadAsync<PersonDto>();

        await Send.OkAsync(new PagedResponse<PersonDto>(people, personCount), ct);
    }
}
