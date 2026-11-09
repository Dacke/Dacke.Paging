using AwesomeAssertions;
using Dacke.DapperPaging.Offset;
using Dacke.DapperPaging.Offset.Filtering;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Filtering;

namespace Dacke.Paging.UnitTests.Filtering;

public class DapperOffsetFilteringExtensionsTests {
    [Fact]
    public void BuildSqlWhere_WithNullOrEmptyFilters_ReturnsEmptyClauseAndNoParameters() {
        // Arrange
        var request = new PagedRequest { Filters = null! };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be(string.Empty);
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void BuildSqlWhere_WithNullRequest_ReturnsEmptyClauseAndNoParameters() {
        // Arrange
        PagedRequest? request = null;

        // Act
        var (whereClause, parameters) = request!.BuildSqlWhere();

        // Assert
        whereClause.Should().Be(string.Empty);
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(MatchMode.Equals, "=", "TestValue")]
    [InlineData(MatchMode.NotEquals, "!=", "TestValue")]
    [InlineData(MatchMode.LessThan, "<", "10")]
    [InlineData(MatchMode.LessThanOrEqualTo, "<=", "10")]
    [InlineData(MatchMode.GreaterThan, ">", "10")]
    [InlineData(MatchMode.GreaterThanOrEqualTo, ">=", "10")]
    public void BuildSqlWhere_WithStandardComparisonMatchModes_GeneratesExpectedSqlAndParameter(MatchMode matchMode, string expectedOperator, string value) {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Constraints = [
                        new ColumnFilter { PropertyName = "Age", MatchMode = matchMode, Value = value }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [Age] " + expectedOperator + " @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be(value);
    }

    [Theory]
    [InlineData(MatchMode.Contains, "LIKE", "%John%")]
    [InlineData(MatchMode.NotContains, "NOT LIKE", "%John%")]
    [InlineData(MatchMode.StartsWith, "LIKE", "John%")]
    [InlineData(MatchMode.EndsWith, "LIKE", "%John")]
    public void BuildSqlWhere_WithStringPatternMatchModes_GeneratesExpectedSqlAndWildcardParameter(MatchMode matchMode, string expectedSqlOperator, string expectedParamValue) {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Constraints = [
                        new ColumnFilter { PropertyName = "FirstName", MatchMode = matchMode, Value = "John" }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be($"WHERE [FirstName] {expectedSqlOperator} @p_filter_0");
        parameters.Get<string>("@p_filter_0").Should().Be(expectedParamValue);
    }

    [Fact]
    public void BuildSqlWhere_WithDefaultFallbackMatchMode_FallsBackToEquals() {
        // Arrange - casting an invalid/unhandled MatchMode int to trigger the default switch branch
        var invalidMatchMode = (MatchMode)999;
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Constraints = [
                        new ColumnFilter { PropertyName = "Status", MatchMode = invalidMatchMode, Value = "Active" }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [Status] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("Active");
    }

    [Fact]
    public void BuildSqlWhere_WithWhitespaceValueAndNonEqualsMatchMode_Skipped() {
        // Arrange - non-Equals match modes with whitespace strings should be skipped
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Constraints = [
                        new ColumnFilter { PropertyName = "FirstName", MatchMode = MatchMode.Contains, Value = "   " }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be(string.Empty);
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void BuildSqlWhere_WithWhitespaceValueAndEqualsMatchMode_Processed() {
        // Arrange - Equals match mode allows whitespace values
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Constraints = [
                        new ColumnFilter { PropertyName = "MiddleName", MatchMode = MatchMode.Equals, Value = "   " }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [MiddleName] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("   ");
    }

    [Fact]
    public void BuildSqlWhere_WithNullFilterRequestsOrConstraintsOrValues_SkippedGracefully() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                null!,
                new FilterRequest { Constraints = null! },
                new FilterRequest { Constraints = [null!] },
                new FilterRequest { Constraints = [new ColumnFilter { PropertyName = "Test", Value = null! }] }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be(string.Empty);
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void BuildSqlWhere_WithMultipleConstraintsUnderAndOperator_CombinesWithAnd() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new ColumnFilter { PropertyName = "FirstName", MatchMode = MatchMode.Equals, Value = "Alice" },
                        new ColumnFilter { PropertyName = "Age", MatchMode = MatchMode.GreaterThan, Value = "25" }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE ([FirstName] = @p_filter_0 AND [Age] > @p_filter_1)");
        parameters.Get<object>("@p_filter_0").Should().Be("Alice");
        parameters.Get<object>("@p_filter_1").Should().Be("25");
    }

    [Fact]
    public void BuildSqlWhere_WithMultipleConstraintsUnderOrOperator_CombinesWithOr() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                    Operator = FilterOperator.Any,
                    Constraints = [
                        new ColumnFilter { PropertyName = "State", MatchMode = MatchMode.Equals, Value = "TX" },
                        new ColumnFilter { PropertyName = "State", MatchMode = MatchMode.Equals, Value = "FL" }
                    ]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE ([State] = @p_filter_0 OR [State] = @p_filter_1)");
        parameters.Get<object>("@p_filter_0").Should().Be("TX");
        parameters.Get<object>("@p_filter_1").Should().Be("FL");
    }

    [Fact]
    public void BuildSqlWhere_WithGlobalFilterOperatorAny_CombinesFilterRequestsWithOr() {
        // Arrange
        var request = new PagedRequest {
            GlobalFilterOperator = FilterOperator.Any,
            Filters = [
                new FilterRequest {
                    Constraints = [new ColumnFilter { PropertyName = "Role", MatchMode = MatchMode.Equals, Value = "Admin" }]
                },
                new FilterRequest {
                    Constraints = [new ColumnFilter { PropertyName = "IsActive", MatchMode = MatchMode.Equals, Value = true.ToString() }]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [Role] = @p_filter_0 OR [IsActive] = @p_filter_1");
    }

    [Fact]
    public void BuildSqlWhere_WithGlobalFilterOperatorAll_CombinesFilterRequestsWithAnd() {
        // Arrange
        var request = new PagedRequest {
            GlobalFilterOperator = FilterOperator.All,
            Filters = [
                new FilterRequest {
                    Constraints = [new ColumnFilter { PropertyName = "Role", MatchMode = MatchMode.Equals, Value = "Admin" }]
                },
                new FilterRequest {
                    Constraints = [new ColumnFilter { PropertyName = "IsActive", MatchMode = MatchMode.Equals, Value = true.ToString() }]
                }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [Role] = @p_filter_0 AND [IsActive] = @p_filter_1");
    }

    [Fact]
    public void BuildSqlWhere_WithMaliciousColumnName_SanitizesColumnCharacters() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                Constraints = [
                    new ColumnFilter { PropertyName = "Age'; DROP TABLE Users; --", MatchMode = MatchMode.Equals, Value = "30" }
                ]
            }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be("WHERE [Age DROP TABLE Users --] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("30");
    }

    [Fact]
    public void BuildSqlWhere_WithSingleConstraintPerFilterRequest_DoesNotWrapInParentheses() {
        // Arrange
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                Operator = FilterOperator.All,
                Constraints = [
                    new ColumnFilter { PropertyName = "Email", MatchMode = MatchMode.Equals, Value = "test@test.com" }
                ]
            }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        // Should be direct without outer parentheses because Count == 1
        whereClause.Should().Be("WHERE [Email] = @p_filter_0");
        parameters.Get<object>("@p_filter_0").Should().Be("test@test.com");
    }

    [Fact]
    public void BuildSqlWhere_WithSkippedConstraints_MaintainsSequentialParameterIndices() {
        // Arrange - first constraint is skipped (null value), second is valid, third is valid
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                Constraints = [
                    new ColumnFilter { PropertyName = "SkippedCol", MatchMode = MatchMode.Equals, Value = null! },
                    new ColumnFilter { PropertyName = "Age", MatchMode = MatchMode.GreaterThan, Value = "20" },
                    new ColumnFilter { PropertyName = "Status", MatchMode = MatchMode.Equals, Value = "Active" }
                ]
            }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        // SkippedCol was skipped, so valid parameters should start cleanly at @p_filter_0 and @p_filter_1
        whereClause.Should().Be("WHERE ([Age] > @p_filter_0 AND [Status] = @p_filter_1)");
        parameters.Get<object>("@p_filter_0").Should().Be("20");
        parameters.Get<object>("@p_filter_1").Should().Be("Active");
    }

    [Fact]
    public void BuildSqlWhere_WithEmptyStringAndNonEqualsMatchMode_Skipped() {
        // Arrange - empty string with NotEquals should be skipped by IsNullOrWhiteSpace check
        var request = new PagedRequest {
            Filters = [
                new FilterRequest {
                Constraints = [
                    new ColumnFilter { PropertyName = "Role", MatchMode = MatchMode.NotEquals, Value = "" }
                ]
            }
            ]
        };

        // Act
        var (whereClause, parameters) = request.BuildSqlWhere();

        // Assert
        whereClause.Should().Be(string.Empty);
        parameters.ParameterNames.Should().BeEmpty();
    }
}