using AwesomeAssertions;
using EFCorePaging.Common.Enums;
using EFCorePaging.Offset.Data;

namespace EFCorePaging.UnitTests.Data;

public class QueryableOffsetExtensionsTests {
    public abstract class TestBase {
        protected readonly IQueryable<TestEntity> TestData = new List<TestEntity> {
            new() { Id = 1, Name = "Alice Smith", Age = 30, Status = Status.Active },
            new() { Id = 2, Name = "Bob Jones", Age = 45, Status = Status.Pending },
            new() { Id = 3, Name = "Charlie Brown", Age = 22, Status = Status.Active },
            new() { Id = 4, Name = "Diana Prince", Age = 35, Status = Status.Suspended }
        }.AsQueryable();
    }

    public class When_SortString_Is_Null_Or_Whitespace : TestBase {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void It_Should_Return_Original_Query_Unchanged(string? sortBy) {
            // Given
            // (Provided via InlineData)

            // When
            var result = TestData.ApplySorting(sortBy!).ToList();

            // Then
            result.Should().HaveCount(4);
            result[0].Id.Should().Be(1); // Maintains original order
        }
    }

    public class When_Sorting_By_Single_Column : TestBase {
        [Fact]
        public void It_Should_Order_Ascending_By_Default_When_Direction_Is_Omitted() {
            // Given
            var sortBy = "Name";

            // When
            var result = TestData.ApplySorting(sortBy).ToList();

            // Then
            result.Select(x => x.Name).Should().BeInAscendingOrder();
            result.Select(x => x.Name).Should().Equal(["Alice Smith", "Bob Jones", "Charlie Brown", "Diana Prince"]);
        }

        [Theory]
        [InlineData("ASC")]
        [InlineData("asc")]
        [InlineData("0")]
        public void It_Should_Order_Ascending_When_Explicit_Asc_Token_Is_Provided(string directionToken) {
            // Given
            var sortBy = $"Age {directionToken}";

            // When
            var result = TestData.ApplySorting(sortBy).ToList();

            // Then
            result.Select(x => x.Age).Should().Equal([22, 30, 35, 45]);
        }

        [Theory]
        [InlineData("DESC")]
        [InlineData("desc")]
        [InlineData("1")]
        public void It_Should_Order_Descending_When_Explicit_Desc_Token_Is_Provided(string directionToken) {
            // Given
            var sortBy = $"Age {directionToken}";

            // When
            var result = TestData.ApplySorting(sortBy).ToList();

            // Then
            result.Select(x => x.Age).Should().Equal([45, 35, 30, 22]);
        }
    }

    public class When_Sorting_By_Multiple_Columns : TestBase {
        [Fact]
        public void It_Should_Apply_Primary_And_Secondary_Sorts_Correctly() {
            // Given: Add entities with duplicate status for multi-column testing
            var customData = new List<TestEntity> {
                new() { Id = 1, Name = "Zack", Age = 30, Status = Status.Active },
                new() { Id = 2, Name = "Aaron", Age = 45, Status = Status.Active },
                new() { Id = 3, Name = "Bob", Age = 22, Status = Status.Pending }
            }.AsQueryable();

            // Sort by Status ASC, then Name ASC
            var sortBy = "Status ASC, Name ASC";

            // When
            var result = customData.ApplySorting(sortBy).ToList();

            // Then
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Bob");   // Status.Pending comes last
            result[1].Name.Should().Be("Aaron"); // Status.Active comes first, then Name Aaron
            result[2].Name.Should().Be("Zack");  // Status.Active comes second, then Name Zack            
        }
    }

    public class When_Using_Property_Mappings : TestBase {
        [Fact]
        public void It_Should_Translate_Mapped_Property_Names() {
            // Given: Map DTO property "FullName" to Entity property "Name"
            var sortBy = "FullName DESC";
            var propertyMap = new Dictionary<string, string> {
                { "FullName", "Name" }
            };

            // When
            var result = TestData.ApplySorting(sortBy, propertyMap: propertyMap).ToList();

            // Then
            result.Select(x => x.Name).Should().Equal(["Diana Prince", "Charlie Brown", "Bob Jones", "Alice Smith"]);
        }
    }

    public class When_Sort_Property_Does_Not_Exist : TestBase {
        [Fact]
        public void It_Should_Throw_Argument_Exception() {
            // Given
            var sortBy = "NonExistentProperty ASC";

            // When
            var action = () => TestData.ApplySorting(sortBy).ToList();

            // Then
            action.Should().Throw<ArgumentException>()
                .WithMessage("*Property 'NonExistentProperty' not found on type TestEntity*");
        }
    }

    public class When_Using_Custom_Default_Direction : TestBase {
        [Fact]
        public void It_Should_Respect_Custom_Default_Direction_When_Omitted() {
            // Given
            var sortBy = "Age"; // No direction specified

            // When
            var result = TestData.ApplySorting(sortBy, defaultDirection: SortDirection.Descending).ToList();

            // Then
            result.Select(x => x.Age).Should().Equal([45, 35, 30, 22]); // Descending order
        }
    }

    public class When_Sort_Direction_Token_Is_Invalid : TestBase {
        [Fact]
        public void It_Should_Fallback_To_Default_Direction_When_Token_Is_Malformed() {
            // Given
            var sortBy = "Age INVALID_DIR";

            // When
            var result = TestData.ApplySorting(sortBy, defaultDirection: SortDirection.Descending).ToList();

            // Then
            result.Select(x => x.Age).Should().Equal([45, 35, 30, 22]); // Fell back to Descending
        }
    }
}