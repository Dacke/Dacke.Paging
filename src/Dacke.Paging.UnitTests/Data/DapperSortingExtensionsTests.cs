using AwesomeAssertions;
using Dacke.DapperPaging.Offset.Data;
using Dacke.Paging.Common.Enums;
using Dacke.DapperPaging.Offset;

namespace Dacke.Paging.UnitTests.Data;

public class DapperSortingExtensionsTests {
    [Fact]
    public void BuildSqlSorting_WithNullOrWhitespaceSortBy_ReturnsEmptyString() {
        // Arrange
        var request = new PagedRequest { SortBy = null! };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void BuildSqlSorting_SingleColumnWithoutDirection_UsesDefaultSortDirection() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "FirstName",
            DefaultSortDirection = SortDirection.Ascending
        };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be("ORDER BY FirstName ASC");
    }

    [Fact]
    public void BuildSqlSorting_SingleColumnWithDescendingDefault_UsesDescendingFallback() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "Age",
            DefaultSortDirection = SortDirection.Descending
        };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be("ORDER BY Age DESC");
    }

    [Fact]
    public void BuildSqlSorting_SingleColumnWithExplicitDirection_OverridesDefault() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "CreatedAt DESC",
            DefaultSortDirection = SortDirection.Ascending
        };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be("ORDER BY CreatedAt DESC");
    }

    [Fact]
    public void BuildSqlSorting_MultipleColumnsMixedDirections_GeneratesCorrectClause() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "LastName ASC, CreatedAt DESC, Age",
            DefaultSortDirection = SortDirection.Ascending
        };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be("ORDER BY LastName ASC, CreatedAt DESC, Age ASC");
    }

    [Fact]
    public void BuildSqlSorting_WithPropertyMap_TranslatesPropertyNames() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "Id DESC, FullName ASC",
            DefaultSortDirection = SortDirection.Ascending
        };

        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
                { "Id", "PersonId" },
                { "FullName", "LastName" }
            };

        // Act
        var result = request.BuildSqlSorting(propertyMap);

        // Assert
        result.Should().Be("ORDER BY PersonId DESC, LastName ASC");
    }

    [Fact]
    public void BuildSqlSorting_WithUnmappedProperty_LeavesNameUntouched() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "UnmappedField DESC",
            DefaultSortDirection = SortDirection.Ascending
        };

        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
                { "Id", "PersonId" }
            };

        // Act
        var result = request.BuildSqlSorting(propertyMap);

        // Assert
        result.Should().Be("ORDER BY UnmappedField DESC");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildSqlSorting_WithEmptyOrWhitespaceSortBy_ReturnsEmptyString(string sortBy) {
        // Arrange
        var request = new PagedRequest { SortBy = sortBy };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void BuildSqlSorting_WithPropertyMapCaseInsensitivity_TranslatesCorrectly() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "id desc, Lastname ASC",
            DefaultSortDirection = SortDirection.Ascending
        };

        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "ID", "PersonId" },
            { "LastName", "Name" }
        };

        // Act
        var result = request.BuildSqlSorting(propertyMap);

        // Assert
        result.Should().Be("ORDER BY PersonId DESC, Name ASC");
    }

    [Fact]
    public void BuildSqlSorting_WithExtraWhitespaceInClauses_TrimsAndFormatsCorrectly() {
        // Arrange
        var request = new PagedRequest {
            SortBy = "  FirstName   ,   Age DESC  ",
            DefaultSortDirection = SortDirection.Ascending
        };

        // Act
        var result = request.BuildSqlSorting();

        // Assert
        result.Should().Be("ORDER BY FirstName ASC, Age DESC");
    }
}