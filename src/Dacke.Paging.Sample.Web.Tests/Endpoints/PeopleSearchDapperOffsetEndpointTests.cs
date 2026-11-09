using AwesomeAssertions;
using Dacke.DapperPaging.Offset;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Filtering;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using Dacke.Paging.Sample.Web.Endpoints;
using Dacke.Paging.Sample.Web.Tests.Extensions;
using FastEndpoints;
using Microsoft.Data.SqlClient;
using NSubstitute;
using Testcontainers.MsSql;

namespace Dacke.Paging.Sample.Web.Tests.Endpoints;

public class PeopleSearchDapperOffsetEndpointTests {
    public class MsSqlDatabaseFixture : IAsyncLifetime {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();
        public string ConnectionString => _msSqlContainer.GetConnectionString();

        public async ValueTask InitializeAsync() {
            await _msSqlContainer.StartAsync();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE People (
                    PersonId UNIQUEIDENTIFIER PRIMARY KEY,
                    FirstName NVARCHAR(100),
                    LastName NVARCHAR(100),
                    Email NVARCHAR(255),
                    PhoneNumber NVARCHAR(50),
                    Address NVARCHAR(255),
                    City NVARCHAR(100),
                    State NVARCHAR(50),
                    PostalCode NVARCHAR(20),
                    Age INT,
                    BirthDay NVARCHAR(50)
                );";
            await command.ExecuteNonQueryAsync();
            await SeedDatabaseAsync(conn);
        }

        private async Task SeedDatabaseAsync(System.Data.IDbConnection db) {
            var people = new List<Person> {
                new() { PersonId = 1.AsGuid(), FirstName = "Alice", LastName = "Smith", Age = 30 },
                new() { PersonId = 2.AsGuid(), FirstName = "Bob", LastName = "Jones", Age = 35 },
                new() { PersonId = 3.AsGuid(), FirstName = "Charlie", LastName = "Brown", Age = 40 },
                new() { PersonId = 4.AsGuid(), FirstName = "Diana", LastName = "Prince", Age = 28 },
                new() { PersonId = 5.AsGuid(), FirstName = "Ethan", LastName = "Hunt", Age = 45 },
                new() { PersonId = 6.AsGuid(), FirstName = "Fiona", LastName = "Gallagher", Age = 25 },
                new() { PersonId = 7.AsGuid(), FirstName = "George", LastName = "Clooney", Age = 60 },
                new() { PersonId = 8.AsGuid(), FirstName = "Hannah", LastName = "Montana", Age = 22 },
                new() { PersonId = 9.AsGuid(), FirstName = "Ian", LastName = "Malcolm", Age = 50 },
                new() { PersonId = 10.AsGuid(), FirstName = "Julia", LastName = "Roberts", Age = 55 }
            };

            foreach (var p in people) {
                using var cmd = db.CreateCommand();
                cmd.CommandText = "INSERT INTO People (PersonId, FirstName, LastName, Age) VALUES (@PersonId, @FirstName, @LastName, @Age)";

                var pId = cmd.CreateParameter();
                pId.ParameterName = "@PersonId";
                pId.Value = p.PersonId;
                cmd.Parameters.Add(pId);

                var fName = cmd.CreateParameter();
                fName.ParameterName = "@FirstName";
                fName.Value = p.FirstName;
                cmd.Parameters.Add(fName);

                var lName = cmd.CreateParameter();
                lName.ParameterName = "@LastName";
                lName.Value = p.LastName;
                cmd.Parameters.Add(lName);

                var age = cmd.CreateParameter();
                age.ParameterName = "@Age";
                age.Value = p.Age;
                cmd.Parameters.Add(age);

                cmd.ExecuteNonQuery();
            }
        }

        public async ValueTask DisposeAsync() {
            await _msSqlContainer.DisposeAsync();
        }
    }

    public abstract class TestBase : IClassFixture<MsSqlDatabaseFixture> {
        protected PeopleSearchDapperOffsetEndpoint endpoint;

        protected TestBase(MsSqlDatabaseFixture fixture) {
            var connectionFactory = Substitute.For<IDbConnectionFactory>();
            connectionFactory.CreateConnection().Returns(_ => {
                var conn = new SqlConnection(fixture.ConnectionString);
                conn.Open();
                return conn;
            });

            endpoint = Factory.Create<PeopleSearchDapperOffsetEndpoint>(connectionFactory);
        }
    }

    public class When_SearchingPeopleWithDapperOffset : TestBase {
        public When_SearchingPeopleWithDapperOffset(MsSqlDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ItShouldReturnPagedResponseWithCorrectTotalCountAndPageSize() {
            var request = new PagedRequest { PageSize = 4, Skip = 0 };
            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(4);
            response.Values[0].Id.Should().Be(1.AsGuid());
            response.Values[1].Id.Should().Be(2.AsGuid());
            response.Values[2].Id.Should().Be(3.AsGuid());
            response.Values[3].Id.Should().Be(4.AsGuid());
        }

        [Fact]
        public async Task ItShouldRespectSkipAndPaginationOffset() {
            var request = new PagedRequest { PageSize = 3, Skip = 4 };
            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(3);
            response.Values[0].Id.Should().Be(5.AsGuid());
            response.Values[1].Id.Should().Be(6.AsGuid());
            response.Values[2].Id.Should().Be(7.AsGuid());
        }

        [Fact]
        public async Task ItShouldApplyFiltersCorrectly() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.Equals, Value = "Smith" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(1);
            response.Values.Should().HaveCount(1);
            response.Values[0].Id.Should().Be(1.AsGuid());
            response.Values[0].LastName.Should().Be("Smith");
        }

        [Fact]
        public async Task ItShouldRespectCustomSorting() {
            var request = new PagedRequest {
                PageSize = 2,
                Skip = 0,
                SortBy = $"{nameof(PersonDto.Age)} desc"
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(7.AsGuid());
            response.Values[1].Id.Should().Be(10.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportContainsMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.Contains, Value = "li" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(3);
            response.Values.Should().HaveCount(3);
        }

        [Fact]
        public async Task ItShouldRespectCustomSortingAscending() {
            var request = new PagedRequest {
                PageSize = 2,
                Skip = 0,
                SortBy = $"{nameof(PersonDto.Age)} asc"
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(8.AsGuid());
            response.Values[1].Id.Should().Be(6.AsGuid());
        }

        [Fact]
        public async Task ItShouldRespectCustomSortingDescending() {
            var request = new PagedRequest {
                PageSize = 2,
                Skip = 0,
                SortBy = $"{nameof(PersonDto.Age)} desc"
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(7.AsGuid());
            response.Values[1].Id.Should().Be(10.AsGuid());
        }

        [Fact]
        public async Task ItShouldReturnEmptyValuesWhenSkipExceedsTotalCount() {
            var request = new PagedRequest { PageSize = 5, Skip = 50 };
            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().BeEmpty();
        }

        [Fact]
        public async Task ItShouldApplyNumericGreaterThanFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.GreaterThan, Value = "50" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(2);
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportMultiColumnSorting() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                SortBy = "Age desc, LastName asc"
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values[0].Id.Should().Be(7.AsGuid());
            response.Values[1].Id.Should().Be(10.AsGuid());
        }

        [Fact]
        public async Task ItShouldHandleEmptyOrNullFiltersGracefully() {
            var request = new PagedRequest { PageSize = 5, Skip = 0, Filters = [] };
            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(5);
        }

        [Fact]
        public async Task ItShouldSupportStartsWithMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.StartsWith, Value = "Al" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(1);
            response.Values[0].FirstName.Should().Be("Alice");
        }

        [Fact]
        public async Task ItShouldSupportNumericLessThanFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.LessThan, Value = "26" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(2);
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportMultipleConstraintsWithAnyOperator() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Operator = FilterOperator.Any,
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.Equals, Value = "Smith" },
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.Equals, Value = "Jones" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(2);
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportNotContainsMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.NotContains, Value = "Smith" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(9);
            response.Values.Should().NotContain(v => v.LastName == "Smith");
        }

        [Fact]
        public async Task ItShouldSupportEndsWithMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.EndsWith, Value = "ie" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(1);
            response.Values.Should().HaveCount(1);
        }

        [Fact]
        public async Task ItShouldSupportNotEqualsMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.NotEquals, Value = "Smith" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(9);
            response.Values.Should().NotContain(v => v.LastName == "Smith");
        }

        [Fact]
        public async Task ItShouldSupportLessThanOrEqualToMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.LessThanOrEqualTo, Value = "25" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(2);
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportGreaterThanOrEqualToMatchModeFilter() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.GreaterThanOrEqualTo, Value = "60" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(1);
            response.Values[0].Id.Should().Be(7.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportMultipleConstraintsWithAllOperator() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Operator = FilterOperator.All,
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.Equals, Value = "Smith" },
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.Equals, Value = "Alice" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(1);
            response.Values[0].Id.Should().Be(1.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportMultipleSeparateFilterGroups() {
            var request = new PagedRequest {
                PageSize = 10,
                Skip = 0,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.NotContains, Value = "Smith" }
                         }
                     },
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.LessThan, Value = "30" }
                         }
                     }
                }
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(3);
            response.Values.Should().HaveCount(3);
        }

        [Fact]
        public async Task ItShouldRespectDefaultSortDirectionWhenSortByIsEmpty() {
            var request = new PagedRequest {
                PageSize = 3,
                Skip = 0,
                SortBy = string.Empty,
                DefaultSortDirection = SortDirection.Descending
            };

            await endpoint.HandleAsync(request, CancellationToken.None);

            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.TotalCount.Should().Be(10);
            response.Values.Should().HaveCount(3);
            response.Values[0].Id.Should().Be(1.AsGuid());
        }
    }
}