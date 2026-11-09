using AwesomeAssertions;
using Dacke.DapperPaging.Cursor;
using Dacke.DapperPaging.Cursor.Sorting;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.UnitTests.Sorting;

public class DapperCursorSortingExtensionsTests {
    [Fact]
    public void BuildCursorSqlSorting_DefaultSort_Forward_GeneratesExpectedClause() {
        // Arrange
        var request = new PagedRequest { SortBy = null!, DefaultSortDirection = SortDirection.Ascending };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName ASC, PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_CustomMultiColumn_Forward_GeneratesExpectedClause() {
        // Arrange
        var request = new PagedRequest { SortBy = "LastName DESC, FirstName ASC" };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName DESC, FirstName ASC, PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_BackwardNavigation_InvertsAllDirections() {
        // Arrange
        var request = new PagedRequest { SortBy = "LastName DESC, FirstName ASC" };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Backward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName ASC, FirstName DESC, PersonId DESC");
    }

    [Fact]
    public void BuildCursorSqlSorting_AppliesPropertyMapping() {
        // Arrange
        var request = new PagedRequest { SortBy = "Id ASC" };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Id", "PersonId" }
        };

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata, propertyMap);

        // Assert
        result.Should().Be("ORDER BY PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_PrimaryKeyAlreadyIncluded_DoesNotDuplicate() {
        // Arrange
        var request = new PagedRequest { SortBy = "LastName ASC, PersonId ASC" };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName ASC, PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_HandlesWhitespaceAndEmptyClausesGracefully() {
        // Arrange
        var request = new PagedRequest { SortBy = "  LastName DESC , , FirstName   " };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName DESC, FirstName ASC, PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_PrimaryKeyAlreadyIncludedExplicitly_DoesNotDuplicate() {
        // Arrange
        var request = new PagedRequest { SortBy = "LastName ASC, PersonId ASC" };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName ASC, PersonId ASC");
    }

    [Fact]
    public void BuildCursorSqlSorting_DefaultSortDirectionDescending_AppliesCorrectly() {
        // Arrange
        var request = new PagedRequest { SortBy = null!, DefaultSortDirection = SortDirection.Descending };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Forward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        result.Should().Be("ORDER BY LastName DESC, PersonId DESC");
    }

    [Fact]
    public void BuildCursorSqlSorting_BackwardNavigation_WithCustomDefaultSortDescending() {
        // Arrange
        var request = new PagedRequest { SortBy = null!, DefaultSortDirection = SortDirection.Descending };
        var metadata = new PagingMetadata("PersonId", "PersonId", "Id") { DefaultSortColumn = "LastName" };
        var pagingContext = new PagingContext(1, null, [], metadata.EntityKeyColumn, NavigationDirection.Backward);

        // Act
        var result = request.BuildCursorSqlSorting(pagingContext, metadata);

        // Assert
        // Backward navigation inverts DESC to ASC
        result.Should().Be("ORDER BY LastName ASC, PersonId ASC");
    }
}