using AwesomeAssertions;
using Dacke.EFCorePaging.Cursor.Data;
using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;

namespace Dacke.Paging.UnitTests.Data;

public class QueryableKeysetExtensionsTests {
    public abstract class TestBase {
        protected readonly IQueryable<TestEntity> TestData = new List<TestEntity> {
            new() { Id = 1, Name = "Alice Smith", Age = 22, Status = Status.Active, UniqueId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new() { Id = 2, Name = "Bob Jones", Age = 30, Status = Status.Pending, UniqueId = Guid.Parse("22222222-2222-2222-2222-222222222222") },
            new() { Id = 3, Name = "Charlie Brown", Age = 30, Status = Status.Active, UniqueId = Guid.Parse("33333333-3333-3333-3333-333333333333") },
            new() { Id = 4, Name = "Diana Prince", Age = 45, Status = Status.Suspended, UniqueId = Guid.Parse("44444444-4444-4444-4444-444444444444") }
        }.AsQueryable();
    }

    public class When_Cursor_Is_Null_Or_Empty : TestBase {
        [Fact]
        public void It_Should_Apply_Ordering_And_Return_Full_Query_When_Cursor_IsNull() {
            // Given
            List<(string, SortDirection)> sortColumns = [("Age", SortDirection.Ascending)];
            string primaryKeyName = "Id";
            var context = new PagingContext(10, null, sortColumns, primaryKeyName, NavigationDirection.Forward);

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            result.Should().HaveCount(4);
            result.Select(x => x.Age).Should().BeInAscendingOrder();
            result[0].Id.Should().Be(1); // Age 22 (Alice)
        }
    }

    public class When_Paging_Forward_With_Cursor : TestBase {
        [Fact]
        public void It_Should_Filter_And_Paginate_Correctly_Using_Single_Column() {
            // Given: Cursor pointing to Bob (Age: 30, Id: 2)
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "2",
                SortValues = new Dictionary<string, string?> { { "Age", "30" } }
            };
            List<(string, SortDirection)> sortColumns = [("Age", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            // Items after Bob (Age 30, Id 2) in ascending order should be Charlie (Age 30, Id 3) and Diana (Age 45, Id 4)
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(3);
            result[1].Id.Should().Be(4);
        }

        [Fact]
        public void It_Should_Handle_Multi_Column_Sort_Keysets() {
            // Given: Sort by Age ASC, then Name ASC. Cursor pointing to Bob (Age: 30, Name: "Bob Jones", Id: 2)
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "2",
                SortValues = new Dictionary<string, string?> {
                    { "Age", "30" },
                    { "Name", "Bob Jones" }
                }
            };
            List<(string, SortDirection)> sortColumns = [
                ("Age", SortDirection.Ascending),
                ("Name", SortDirection.Ascending)
            ];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            // Following Bob (Age 30, Name "Bob Jones"): Charlie Brown (Age 30, Name "Charlie Brown") and Diana Prince (Age 45)
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(3); // Charlie
            result[1].Id.Should().Be(4); // Diana
        }
    }

    public class When_Paging_Backward_With_Cursor : TestBase {
        [Fact]
        public void It_Should_Invert_Sorts_And_Filter_Correctly_For_Backward_Traversal() {
            // Given: Backward paging from Charlie (Age: 30, Id: 3)
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "3",
                SortValues = new Dictionary<string, string?> { { "Age", "30" } }
            };
            List<(string, SortDirection)> sortColumns = [("Age", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id", NavigationDirection.Backward);

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            // Going backwards from Charlie (Age 30, Id 3) should yield items before it: Bob (Age 30, Id 2) and Alice (Age 22, Id 1)
            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([1, 2]);
        }
    }

    public class When_Property_Does_Not_Exist : TestBase {
        [Fact]
        public void It_Should_Throw_Argument_Exception() {
            // Given
            var cursor = new KeysetCursor { PrimaryKeyName = "Id", PrimaryKeyValue = "1" };
            List<(string, SortDirection)> sortColumns = [("NonExistentColumn", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var action = () => TestData.ApplyKeysetPaging(context).ToList();

            // Then
            action.Should().Throw<ArgumentException>()
                .WithMessage("*Property 'NonExistentColumn' not found*");
        }
    }

    public class When_Paging_With_String_And_Guid_Properties : TestBase {
        [Fact]
        public void It_Should_Handle_String_Comparison_Keysets_Correctly() {
            // Given: Sort by Name ASC. Cursor pointing to "Bob Jones"
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "2",
                SortValues = new Dictionary<string, string?> { { "Name", "Bob Jones" } }
            };
            List<(string, SortDirection)> sortColumns = [("Name", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Charlie Brown");
            result[1].Name.Should().Be("Diana Prince");
        }

        [Fact]
        public void It_Should_Handle_Guid_Comparison_Keysets_Correctly() {
            // Given: Sort by UniqueId ASC. Cursor pointing to Bob's unique ID ("22222222-2222-2222-2222-222222222222")
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "2",
                SortValues = new Dictionary<string, string?> { { "UniqueId", "22222222-2222-2222-2222-222222222222" } }
            };
            List<(string, SortDirection)> sortColumns = [("UniqueId", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var result = TestData.ApplyKeysetPaging(context).ToList();

            // Then
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(3); // Charlie
            result[1].Id.Should().Be(4); // Diana
        }
    }

    public class When_Primary_Key_Is_Omitted_From_Sort_Columns : TestBase {
        [Fact]
        public void It_Should_Automatically_Append_Primary_Key_For_Deterministic_Ordering() {
            // Given: Sort columns only specifies "Age", omitting "Id"
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "2",
                SortValues = new Dictionary<string, string?> { { "Age", "30" } }
            };
            List<(string, SortDirection)> sortColumns = [("Age", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            // This exercises the fallback that checks if primaryKeyName is in sortColumns and appends it
            var action = () => TestData.ApplyKeysetPaging(context).ToList();

            // Then
            action.Should().NotThrow();
            var result = action();
            result.Should().NotBeEmpty();
        }
    }

    public class When_Keyset_Comparison_Type_Is_Unsupported : TestBase {
        [Fact]
        public void It_Should_Throw_Not_Supported_Exception_For_Invalid_String_Comparison() {
            // Given: Access the private static BuildComparison method via reflection
            var method = typeof(QueryableKeysetExtensions)
                .GetMethod("BuildComparison", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(typeof(TestEntity));

            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TestEntity), "x");
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "1",
                SortValues = new Dictionary<string, string?> { { "Name", "Alice" } }
            };

            // Pass an explicitly unsupported ExpressionType (e.g., ExpressionType.Add) for a string property
            System.Linq.Expressions.ExpressionType unsupportedType = System.Linq.Expressions.ExpressionType.Add;

            // When
            var action = () => method.Invoke(null, [parameter, cursor, "Name", unsupportedType]);

            // Then
            action.Should().Throw<System.Reflection.TargetInvocationException>()
                .WithInnerException<NotSupportedException>()
                .WithMessage("*Comparison type Add is not supported for string*");
        }
    }

    public class When_Cursor_Value_Conversion_Fails : TestBase {
        [Fact]
        public void It_Should_Throw_Exception_When_Parsing_Malformed_Guid_In_Cursor() {
            // Given: Cursor with an invalid Guid string format for a Guid property
            var cursor = new KeysetCursor {
                PrimaryKeyName = "Id",
                PrimaryKeyValue = "1",
                SortValues = new Dictionary<string, string?> { { "UniqueId", "not-a-valid-guid-string" } }
            };
            List<(string, SortDirection)> sortColumns = [("UniqueId", SortDirection.Ascending)];
            var context = new PagingContext(20, cursor, sortColumns, "Id");

            // When
            var action = () => TestData.ApplyKeysetPaging(context).ToList();

            // Then
            action.Should().Throw<FormatException>(); // Guid.Parse fails on malformed input
        }
    }
}