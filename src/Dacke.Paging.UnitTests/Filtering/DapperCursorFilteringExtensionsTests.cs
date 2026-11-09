using AwesomeAssertions;
using Dacke.DapperPaging.Cursor;
using Dacke.DapperPaging.Cursor.Filtering;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Filtering;
using Dacke.Paging.Common.Interfaces;

namespace Dacke.Paging.UnitTests.Filtering;

public class DapperCursorFilteringExtensionsTests {
    private readonly Dictionary<string, string> _defaultPropertyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Id", "PersonId" },
        { "FirstName", "FirstName" },
        { "LastName", "LastName" },
        { "Age", "Age" }
    };

    [Fact]
    public void BuildSqlWhere_NullRequest_ThrowsArgumentNullException() {
        // Arrange
        IFilterableRequest? request = null;
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var act = () => request!.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildSqlWhere_NoFiltersOrCursor_ReturnsEmptyClauseAndEmptyParameters() {
        // Arrange
        var request = new PagedRequest();
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().BeEmpty();
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(MatchMode.Equals, "=", "50")]
    [InlineData(MatchMode.NotEquals, "!=", "50")]
    [InlineData(MatchMode.Contains, "LIKE", "%50%")]
    [InlineData(MatchMode.NotContains, "NOT LIKE", "%50%")]
    [InlineData(MatchMode.StartsWith, "LIKE", "50%")]
    [InlineData(MatchMode.EndsWith, "LIKE", "%50")]
    [InlineData(MatchMode.LessThan, "<", "50")]
    [InlineData(MatchMode.LessThanOrEqualTo, "<=", "50")]
    [InlineData(MatchMode.GreaterThan, ">", "50")]
    [InlineData(MatchMode.GreaterThanOrEqualTo, ">=", "50")]
    public void BuildSqlWhere_MatchModes_GeneratesExpectedSqlAndParameters(MatchMode matchMode, string expectedOperator, object expectedParamValue) {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Operator = FilterOperator.All,
                    Constraints = new List<ColumnFilter> {
                        new() { PropertyName = "Age", MatchMode = matchMode, Value = "50" }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be($"WHERE [Age] {expectedOperator} @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be(expectedParamValue);
    }

    [Fact]
    public void BuildSqlWhere_MultipleConstraintsInFilter_OperatorAll_CombinesWithAnd() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Operator = FilterOperator.All,
                    Constraints = new List<ColumnFilter> {
                        new() { PropertyName = "FirstName", MatchMode = MatchMode.StartsWith, Value = "Jos" },
                        new() { PropertyName = "Age", MatchMode = MatchMode.GreaterThan, Value = "50" }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE ([FirstName] LIKE @p_filter_0 AND [Age] > @p_filter_1)");
        parameters.Get<object>("@p_filter_0").Should().Be("Jos%");
        parameters.Get<object>("@p_filter_1").Should().Be("50");
    }

    [Fact]
    public void BuildSqlWhere_MultipleConstraintsInFilter_OperatorAny_CombinesWithOr() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Operator = FilterOperator.Any,
                    Constraints = new List<ColumnFilter> {
                        new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Joe" },
                        new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Bob" }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE ([FirstName] = @p_filter_0 OR [FirstName] = @p_filter_1)");
    }

    [Fact]
    public void BuildSqlWhere_GlobalFilterOperatorAny_CombinesFilterRequestsWithOr() {
        // Arrange
        var request = new PagedRequest {
            GlobalFilterOperator = FilterOperator.Any,
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Joe" } }
                },
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Bob" } }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [FirstName] = @p_filter_0 OR [FirstName] = @p_filter_1");
    }

    [Fact]
    public void BuildSqlWhere_PropertyMapping_TranslatesColumnNames() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "Id", MatchMode = MatchMode.Equals, Value = "123" } }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [PersonId] = @p_filter_0");
    }

    [Fact]
    public void BuildSqlWhere_SanitizesColumnNamesAgainstSqlInjection() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "Age; DROP TABLE People--", MatchMode = MatchMode.Equals, Value = "20" } }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().NotContain(";");
        whereClause.Should().NotContain("'");
        whereClause.Should().Be("WHERE [Age DROP TABLE People--] = @p_filter_0");
    }

    [Fact]
    public void BuildSqlWhere_CursorForwardNavigation_GeneratesCorrectTupleComparison() {
        // Arrange
        var request = new PagedRequest();
        var sortColumns = new List<(string PropertyName, SortDirection Direction)> {
            ("LastName", SortDirection.Ascending)
        };

        var testGuid = Guid.NewGuid();
        var cursor = new KeysetCursor(
            primaryKeyName: "PersonId",
            primaryKeyValue: testGuid,
            sortValues: [("LastName", (object)"Anderson")]
        );

        var pagingContext = new PagingContext(1, cursor, sortColumns, "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Contain("[LastName] > @p_cursor_val_0");
        whereClause.Should().Contain("[PersonId] > @p_cursor_val_1");
        parameters.Get<object>("@p_cursor_val_0").Should().Be("Anderson");
        parameters.Get<object>("@p_cursor_val_1").Should().BeOfType<Guid>();
        parameters.Get<Guid>("@p_cursor_val_1").Should().Be(testGuid);
    }

    [Fact]
    public void BuildSqlWhere_CursorBackwardNavigation_InvertsInequalityOperators() {
        // Arrange
        var request = new PagedRequest();
        var sortColumns = new List<(string PropertyName, SortDirection Direction)> {
            ("LastName", SortDirection.Ascending)
        };

        var cursor = new KeysetCursor(
            primaryKeyName: "PersonId",
            primaryKeyValue: Guid.NewGuid(),
            sortValues: [("LastName", (object)"Anderson")]
        );

        var pagingContext = new PagingContext(1, cursor, sortColumns, "PersonId", NavigationDirection.Backward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Contain("[LastName] < @p_cursor_val_0");
        whereClause.Should().Contain("[PersonId] < @p_cursor_val_1");
    }

    [Fact]
    public void BuildSqlWhere_CursorStringGuidParsedToGuidType() {
        // Arrange
        var request = new PagedRequest();
        var testGuid = Guid.NewGuid();

        var cursor = new KeysetCursor {
            PrimaryKeyName = "PersonId",
            PrimaryKeyValue = testGuid.ToString(),
            SortValues = new Dictionary<string, string?>()
        };

        var pagingContext = new PagingContext(1, cursor, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (_, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        parameters.Get<object>("@p_cursor_val_0").Should().BeOfType<Guid>();
        parameters.Get<object>("@p_cursor_val_0").Should().Be(testGuid);
    }

    [Fact]
    public void BuildSqlWhere_CombinedFilterAndCursor_GeneratesNestedWhereClause() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "Age", MatchMode = MatchMode.GreaterThan, Value = "30" } }
                }
            }
        };

        var cursor = new KeysetCursor(
            primaryKeyName: "PersonId",
            primaryKeyValue: Guid.NewGuid(),
            sortValues: [("LastName", (object)"Smith")]
        );

        var sortColumns = new List<(string PropertyName, SortDirection Direction)> {
            ("LastName", SortDirection.Ascending)
        };

        var pagingContext = new PagingContext(1, cursor, sortColumns, "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().StartWith("WHERE [Age] > @p_filter_0 AND ([LastName] > @p_cursor_val_1");
    }

    [Fact]
    public void BuildSqlWhere_MultiColumnCursor_GeneratesPrecedingEqualityAndInequalityTree() {
        // Arrange
        var request = new PagedRequest();
        var sortColumns = new List<(string PropertyName, SortDirection Direction)> {
            ("LastName", SortDirection.Ascending),
            ("FirstName", SortDirection.Ascending)
        };

        var cursor = new KeysetCursor(
            primaryKeyName: "PersonId",
            primaryKeyValue: Guid.NewGuid(),
            sortValues: [("LastName", (object)"Smith"), ("FirstName", (object)"John")]
        );

        var pagingContext = new PagingContext(1, cursor, sortColumns, "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        // Multi-column cursor tuple logic should produce an OR clause with nested AND equality checks for preceding columns
        whereClause.Should().Contain("[LastName] > @p_cursor_val_0");
        whereClause.Should().Contain("([LastName] = @p_cursor_val_0 AND [FirstName] > @p_cursor_val_1)");
        whereClause.Should().Contain("([LastName] = @p_cursor_val_0 AND [FirstName] = @p_cursor_val_1 AND [PersonId] > @p_cursor_val_2)");
    }

    [Fact]
    public void BuildSqlWhere_SkipsWhitespaceValuesForNonEqualsMatchModes() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> {
                        // Whitespace with StartsWith should be skipped
                        new() { PropertyName = "FirstName", MatchMode = MatchMode.StartsWith, Value = "   " }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().BeEmpty();
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void BuildSqlWhere_AllowsWhitespaceValueForEqualsMatchMode() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> {
                        // Whitespace with Equals should be kept
                        new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "   " }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [FirstName] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("   ");
    }

    [Fact]
    public void BuildSqlWhere_CursorWithoutSortValues_GeneratesOnlyPrimaryKeyTieBreaker() {
        // Arrange
        var request = new PagedRequest();
        var testGuid = Guid.NewGuid();
        var cursor = new KeysetCursor {
            PrimaryKeyName = "PersonId",
            PrimaryKeyValue = testGuid.ToString(),
            SortValues = null
        };
        var pagingContext = new PagingContext(1, cursor, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [PersonId] > @p_cursor_val_0");
        parameters.Get<object>("@p_cursor_val_0").Should().BeOfType<Guid>();
    }

    [Fact]
    public void BuildSqlWhere_HandlesNullFiltersOrNullConstraintsGracefully() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                null!,
                new FilterRequest { Constraints = null! },
                new FilterRequest { Constraints = [null!] }
            ]
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().BeEmpty();
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void BuildSqlWhere_UnrecognizedMatchMode_DefaultsToEquals() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> {
                        // Forcing an out-of-bounds enum value to test the default switch branch
                        new() { PropertyName = "Age", MatchMode = (MatchMode)999, Value = "25" }
                    }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [Age] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("25");
    }

    [Fact]
    public void BuildSqlWhere_PropertyNotInMap_FallsBackToOriginalPropertyName() {
        // Arrange
        var request = new PagedRequest {
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "UnmappedProperty", MatchMode = MatchMode.Equals, Value = "Test" } }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);
        var partialMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, partialMap);

        // Assert
        whereClause.Should().Be("WHERE [UnmappedProperty] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("Test");
    }

    [Fact]
    public void BuildSqlWhere_GlobalFilterOperatorAll_CombinesFilterRequestsWithAnd() {
        // Arrange
        var request = new PagedRequest {
            GlobalFilterOperator = FilterOperator.All,
            Filters = new List<FilterRequest> {
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Joe" } }
                },
                new FilterRequest {
                    Constraints = new List<ColumnFilter> { new() { PropertyName = "LastName", MatchMode = MatchMode.Equals, Value = "Smith" } }
                }
            }
        };
        var pagingContext = new PagingContext(1, null, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, _) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [FirstName] = @p_filter_0 AND [LastName] = @p_filter_1");
    }

    [Fact]
    public void BuildSqlWhere_CursorWithEmptySortValuesDictionary_GeneratesOnlyPrimaryKeyTieBreaker() {
        // Arrange
        var request = new PagedRequest();
        var testGuid = Guid.NewGuid();
        var cursor = new KeysetCursor {
            PrimaryKeyName = "PersonId",
            PrimaryKeyValue = testGuid.ToString(),
            SortValues = new Dictionary<string, string?>()
        };
        var pagingContext = new PagingContext(1, cursor, [], "PersonId", NavigationDirection.Forward);

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere(pagingContext, _defaultPropertyMap);

        // Assert
        whereClause.Should().Be("WHERE [PersonId] > @p_cursor_val_0");
        parameters.Get<object>("@p_cursor_val_0").Should().BeOfType<Guid>();
    }
}