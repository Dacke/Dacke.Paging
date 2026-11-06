using AwesomeAssertions;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Sorting;

namespace Dacke.Paging.UnitTests.Sorting;

public class SortParserTests {
    public class When_SortString_Is_Null_Or_Whitespace {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void It_Should_Return_Default_Column_And_Direction(string? sortBy) {
            // Given
            // (Provided via InlineData parameters)

            // When
            var result = SortParser.ParseSortColumns(sortBy, defaultColumn: "LastName");

            // Then
            result.Should().ContainSingle()
                .Which.Should().Be(("LastName", SortDirection.Ascending));
        }

        [Fact]
        public void It_Should_Respect_Custom_Defaults() {
            // Given
            string? sortBy = null;
            var customDefaultDirection = SortDirection.Descending;
            string customDefaultColumn = "CreatedAt";

            // When
            var result = SortParser.ParseSortColumns(sortBy, customDefaultDirection, customDefaultColumn);

            // Then
            result.Should().ContainSingle()
                .Which.Should().Be((customDefaultColumn, customDefaultDirection));
        }
    }

    public class When_SortString_Has_Single_Column {
        [Fact]
        public void It_Should_Use_Default_Direction_When_Direction_Is_Omitted() {
            // Given
            var sortBy = "FirstName";

            // When
            var result = SortParser.ParseSortColumns(sortBy, SortDirection.Ascending, "LastName");

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Ascending);
        }

        [Fact]
        public void It_Should_Parse_Explicit_Ascending_Direction() {
            // Given
            var sortBy = "FirstName ASC";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Ascending);
        }

        [Theory]
        [InlineData("DESC")]
        [InlineData("Desc")]
        [InlineData("desc")]
        public void It_Should_Parse_Explicit_Descending_Direction_Case_Insensitively(string directionKeyword) {
            // Given
            var sortBy = $"FirstName {directionKeyword}";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Descending);
        }
    }

    public class When_SortString_Has_Multiple_Columns {
        [Fact]
        public void It_Should_Parse_Multiple_Comma_Separated_Columns_Correctly() {
            // Given
            var sortBy = "LastName ASC, FirstName DESC, CreatedAt";

            // When
            var result = SortParser.ParseSortColumns(sortBy, SortDirection.Ascending, "DefaultCol");

            // Then
            result.Should().HaveCount(3);

            result[0].PropertyName.Should().Be("LastName");
            result[0].Direction.Should().Be(SortDirection.Ascending);

            result[1].PropertyName.Should().Be("FirstName");
            result[1].Direction.Should().Be(SortDirection.Descending);

            result[2].PropertyName.Should().Be("CreatedAt");
            result[2].Direction.Should().Be(SortDirection.Ascending); // Falls back to default direction
        }

        [Fact]
        public void It_Should_Handle_Extra_Whitespace_Gracefully() {
            // Given
            var sortBy = "   LastName   DESC  ,   FirstName   ASC   ";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().HaveCount(2);

            result[0].PropertyName.Should().Be("LastName");
            result[0].Direction.Should().Be(SortDirection.Descending);

            result[1].PropertyName.Should().Be("FirstName");
            result[1].Direction.Should().Be(SortDirection.Ascending);
        }
    }

    public class When_SortString_Has_Invalid_Or_Malformed_Tokens {
        [Fact]
        public void It_Should_Fallback_To_Default_Direction_When_Direction_Token_Is_Invalid() {
            // Given
            var sortBy = "FirstName INVALID_DIR";

            // When
            // Passing a custom default direction of Descending to verify fallback behavior
            var result = SortParser.ParseSortColumns(sortBy, SortDirection.Descending, "LastName");

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Descending); // Falls back to default
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        [InlineData("desc")]
        [InlineData("DESC")]
        [InlineData("Desc")]
        public void It_Should_Parse_Explicit_Directions_Case_Insensitively(string directionKeyword) {
            // Given
            var sortBy = $"FirstName {directionKeyword}";
            var expectedDirection = directionKeyword.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(expectedDirection);
        }

        [Fact]
        public void It_Should_Respect_Custom_Default_Direction_For_Single_Column_Without_Explicit_Direction() {
            // Given
            var sortBy = "FirstName"; // No direction specified

            // When
            var result = SortParser.ParseSortColumns(sortBy, SortDirection.Descending, "LastName");

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Descending);
        }
    }

    public class When_SortString_Has_Malformed_Commas {
        [Fact]
        public void It_Should_Ignore_Empty_Segments_Caused_By_Extra_Or_Trailing_Commas() {
            // Given
            var sortBy = ", LastName ASC, , FirstName DESC, ";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().HaveCount(2);

            result[0].PropertyName.Should().Be("LastName");
            result[0].Direction.Should().Be(SortDirection.Ascending);

            result[1].PropertyName.Should().Be("FirstName");
            result[1].Direction.Should().Be(SortDirection.Descending);
        }
    }

    public class When_SortString_Contains_Complex_Formatting {
        [Fact]
        public void It_Should_Handle_Property_Names_With_Underscores_And_Numbers() {
            // Given
            var sortBy = "User_Address_Line1 DESC, Postal_Code_99 ASC";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().HaveCount(2);

            result[0].PropertyName.Should().Be("User_Address_Line1");
            result[0].Direction.Should().Be(SortDirection.Descending);

            result[1].PropertyName.Should().Be("Postal_Code_99");
            result[1].Direction.Should().Be(SortDirection.Ascending);
        }

        [Fact]
        public void It_Should_Handle_Multiple_Internal_Spaces_Between_Property_And_Direction() {
            // Given
            var sortBy = "FirstName     DESC";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().ContainSingle();
            result[0].PropertyName.Should().Be("FirstName");
            result[0].Direction.Should().Be(SortDirection.Descending);
        }
    }

    public class When_SortString_Contains_Only_Commas {
        [Fact]
        public void It_Should_Return_Empty_List_When_All_Segments_Are_Empty() {
            // Given
            var sortBy = ", , ,";

            // When
            var result = SortParser.ParseSortColumns(sortBy);

            // Then
            result.Should().BeEmpty();
        }
    }
}
