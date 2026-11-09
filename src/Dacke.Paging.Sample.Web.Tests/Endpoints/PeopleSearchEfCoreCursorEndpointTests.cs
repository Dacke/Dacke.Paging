using AwesomeAssertions;
using Dacke.EFCorePaging.Cursor;
using Dacke.Paging.Common.Builders;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Filtering;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Sample.Web.Data;
using Dacke.Paging.Sample.Web.Data.Models;
using Dacke.Paging.Sample.Web.Dtos;
using Dacke.Paging.Sample.Web.Tests.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Dacke.Paging.Sample.Web.Tests.Endpoints;

public class PeopleSearchEfCoreCursorEndpointTests {
    [Collection(GlobalConstants.UnitTestCollection)]
    public abstract class TestBase : IDisposable {
        protected PeopleSearchEfCoreCursorEndpoint endpoint;
        protected DbContextOptions<TestDataContext> dbContextOptions;

        protected TestBase() {
            dbContextOptions = new DbContextOptionsBuilder<TestDataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var seedDb = new TestDataContext(dbContextOptions)) {
                SeedDatabase(seedDb);
            }

            var dbContextFactory = Substitute.For<ITestDataContextFactory>();
            dbContextFactory.CreateDbContext().Returns(_ => new TestDataContext(dbContextOptions));
                        
            endpoint = Factory.Create<PeopleSearchEfCoreCursorEndpoint>(dbContextFactory, new CursorPagedResponseBuilder());
        }

        private void SeedDatabase(TestDataContext db) {
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

            db.People.AddRange(people);
            db.SaveChanges();
        }

        public void Dispose() {
            using var cleanupDb = new TestDataContext(dbContextOptions);
            cleanupDb.Database.EnsureDeleted();
        }
    }

    public class When_SearchingPeopleWithCursor : TestBase {
        [Fact]
        public async Task ItShouldReturnPagedResponseWithCorrectPageSizeAndPaginationFlags() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 4,
                Cursor = null
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(4);
            response.HasMore.Should().BeTrue();
            response.NextCursor.Should().NotBeNullOrEmpty();
            response.HasPrevious.Should().BeFalse();
            response.PreviousCursor.Should().BeNull();

            response.Values[0].Id.Should().Be(1.AsGuid());
            response.Values[1].Id.Should().Be(2.AsGuid());
            response.Values[2].Id.Should().Be(3.AsGuid());
            response.Values[3].Id.Should().Be(4.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportNextCursorNavigation() {
            // Arrange - First page
            var request1 = new PagedRequest {
                PageSize = 4,
                Cursor = null
            };

            await endpoint.HandleAsync(request1, CancellationToken.None);
            var firstPageResponse = endpoint.Response;

            // Arrange - Second page using NextCursor
            var request2 = new PagedRequest {
                PageSize = 4,
                Cursor = firstPageResponse.NextCursor,
                NavigationDirection = NavigationDirection.Forward
            };

            // Act
            await endpoint.HandleAsync(request2, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(4);
            response.HasMore.Should().BeTrue();
            response.Values[0].Id.Should().Be(5.AsGuid());
            response.Values[1].Id.Should().Be(6.AsGuid());
            response.Values[2].Id.Should().Be(7.AsGuid());
            response.Values[3].Id.Should().Be(8.AsGuid());
        }

        [Fact]
        public async Task ItShouldApplyFiltersCorrectly() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.Equals, Value = "Smith" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(1);
            response.HasMore.Should().BeFalse();
            response.Values[0].Id.Should().Be(1.AsGuid());
            response.Values[0].LastName.Should().Be("Smith");
        }

        [Fact]
        public async Task ItShouldRespectCustomSorting() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 2,
                Cursor = null,
                SortBy = $"{nameof(PersonDto.Age)} desc"
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(7.AsGuid()); // Age 60 (George Clooney)
            response.Values[1].Id.Should().Be(10.AsGuid()); // Age 55 (Julia Roberts)
        }

        [Fact]
        public async Task ItShouldSupportContainsMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.Contains, Value = "li" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(3);
        }

        [Fact]
        public async Task ItShouldRespectCustomSortingAscending() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 2,
                Cursor = null,
                SortBy = $"{nameof(PersonDto.Age)} asc"
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(8.AsGuid());
            response.Values[1].Id.Should().Be(6.AsGuid());
        }

        [Fact]
        public async Task ItShouldRespectCustomSortingDescending() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 2,
                Cursor = null,
                SortBy = $"{nameof(PersonDto.Age)} desc"
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
            response.Values[0].Id.Should().Be(7.AsGuid());
            response.Values[1].Id.Should().Be(10.AsGuid());
        }

        [Fact]
        public async Task ItShouldApplyNumericGreaterThanFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.GreaterThan, Value = "50" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportMultiColumnSorting() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                SortBy = "Age desc, LastName asc"
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(10);
            response.Values[0].Id.Should().Be(7.AsGuid());
            response.Values[1].Id.Should().Be(10.AsGuid());
        }

        [Fact]
        public async Task ItShouldHandleEmptyOrNullFiltersGracefully() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 5,
                Cursor = null,
                Filters = []
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(5);
        }

        [Fact]
        public async Task ItShouldSupportStartsWithMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.StartsWith, Value = "Al" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(1);
            response.Values[0].FirstName.Should().Be("Alice");
        }

        [Fact]
        public async Task ItShouldSupportNumericLessThanFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.LessThan, Value = "26" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportMultipleConstraintsWithAnyOperator() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
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

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportNotContainsMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.NotContains, Value = "Smith" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().NotContain(v => v.LastName == "Smith");
        }

        [Fact]
        public async Task ItShouldSupportEndsWithMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.FirstName), MatchMode = MatchMode.EndsWith, Value = "ie" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(1);
        }

        [Fact]
        public async Task ItShouldSupportNotEqualsMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.LastName), MatchMode = MatchMode.NotEquals, Value = "Smith" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().NotContain(v => v.LastName == "Smith");
        }

        [Fact]
        public async Task ItShouldSupportLessThanOrEqualToMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.LessThanOrEqualTo, Value = "25" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task ItShouldSupportGreaterThanOrEqualToMatchModeFilter() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
                Filters = new List<FilterRequest> {
                     new() {
                         Constraints = new List<ColumnFilter> {
                             new() { PropertyName = nameof(PersonDto.Age), MatchMode = MatchMode.GreaterThanOrEqualTo, Value = "60" }
                         }
                     }
                }
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values[0].Id.Should().Be(7.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportMultipleConstraintsWithAllOperator() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
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

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values[0].Id.Should().Be(1.AsGuid());
        }

        [Fact]
        public async Task ItShouldSupportMultipleSeparateFilterGroups() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 10,
                Cursor = null,
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

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(3);
        }

        [Fact]
        public async Task ItShouldRespectDefaultSortDirectionWhenSortByIsEmpty() {
            // Arrange
            var request = new PagedRequest {
                PageSize = 3,
                Cursor = null,
                SortBy = string.Empty,
                DefaultSortDirection = SortDirection.Descending
            };

            // Act
            await endpoint.HandleAsync(request, CancellationToken.None);

            // Assert
            var response = endpoint.Response;
            response.Should().NotBeNull();
            response.Values.Should().HaveCount(3);
            response.Values[0].Id.Should().Be(10.AsGuid()); // Descending keyset sort starts from highest ID
        }
    }
}