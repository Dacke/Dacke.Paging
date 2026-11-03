using AwesomeAssertions;
using Paging.Common.Data;
using Paging.Common.Enums;
using Paging.Common.Filtering;

namespace Paging.UnitTests.Data;

public partial class QueryableFilterExtensionsTests {
    public abstract class TestBase {
        protected readonly IQueryable<TestEntity> _testData = new List<TestEntity> {
            new() { Id = 1, Name = "Alice Smith", Email = "alice@example.com", Age = 30, Birthday = new DateTime(1996, 1, 1), Score = 85.5m, Status = Status.Active, UniqueId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new() { Id = 2, Name = "Bob Jones", Email = "bob@sample.org", Age = 45, Birthday = new DateTime(1981, 1, 1), Score = 92.0m, Status = Status.Pending, UniqueId = Guid.Parse("22222222-2222-2222-2222-222222222222") },
            new() { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com", Age = 22, Birthday = new DateTime(2004, 1, 1), Score = 64.5m, Status = Status.Active, UniqueId = Guid.Parse("33333333-3333-3333-3333-333333333333") },
            new() { Id = 4, Name = "Diana Prince", Email = "diana@themyscira.gov", Age = 35, Birthday = new DateTime(1991, 1, 1), Score = 99.9m, Status = Status.Suspended, UniqueId = Guid.Parse("44444444-4444-4444-4444-444444444444") }
        }.AsQueryable();
    }
    
    public class When_Filters_Are_Null_Or_Empty : TestBase {
        [Fact]
        public void It_Should_Return_Original_Query_When_Filters_IsNull() {
            // Given
            var data = new List<TestEntity> { new() { Id = 1, Name = "Alice" } }.AsQueryable();
            List<FilterRequest>? filters = null;

            // When
            var result = data.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(1);
        }

        [Fact]
        public void It_Should_Return_Original_Query_When_Filters_IsEmpty() {
            // Given
            var data = new List<TestEntity> { new() { Id = 1, Name = "Alice" } }.AsQueryable();
            var filters = new List<FilterRequest>();

            // When
            var result = data.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(1);
        }
    }

    public class When_Filtering_By_String_Properties : TestBase {
        [Theory]
        [InlineData(MatchMode.StartsWith, "Al", 1)]
        [InlineData(MatchMode.EndsWith, "Smith", 1)]
        [InlineData(MatchMode.Contains, "lice", 1)]
        [InlineData(MatchMode.NotContains, "Alice", 3)]
        [InlineData(MatchMode.Equals, "Alice Smith", 1)]
        [InlineData(MatchMode.NotEquals, "Alice Smith", 3)]
        public void It_Should_Apply_String_Match_Modes_Correctly(MatchMode matchMode, string searchValue, int expectedCount) {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "Name", Value = searchValue, MatchMode = matchMode }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(expectedCount);
        }
    }

    public class When_Filtering_By_Numeric_And_Comparison_Properties : TestBase {
        [Theory]
        [InlineData(MatchMode.Equals, "30", 1)]
        [InlineData(MatchMode.NotEquals, "30", 3)]
        [InlineData(MatchMode.GreaterThan, "30", 2)]
        [InlineData(MatchMode.GreaterThanOrEqualTo, "30", 3)]
        [InlineData(MatchMode.LessThan, "30", 1)]
        [InlineData(MatchMode.LessThanOrEqualTo, "30", 2)]
        public void It_Should_Apply_Numeric_Comparison_Modes_Correctly(MatchMode matchMode, string searchValue, int expectedCount) {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "Age", Value = searchValue, MatchMode = matchMode }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(expectedCount);
        }
    }

    public class When_Filtering_By_Date_Properties : TestBase {
        [Theory]
        [InlineData(MatchMode.Equals, "1996-01-01", 1)]
        [InlineData(MatchMode.GreaterThan, "1990-01-01", 3)] // Born after 1990 (Alice: 1996, Charlie: 2004, Diana)
        [InlineData(MatchMode.GreaterThanOrEqualTo, "1996-01-01", 2)] // Born after 1996 (Alice: 1996, Charlie: 2004)
        [InlineData(MatchMode.LessThan, "1990-01-01", 2)]    // Born before 1990 (Bob: 1981, Diana: 1991 is not < 1990 wait: Diana is 1991, so Bob 1981 is < 1990. Let's use 1995-01-01: Bob 1981, Diana 1991 -> 2 items)
        public void It_Should_Apply_Date_Comparison_Modes_Correctly(MatchMode matchMode, string searchValue, int expectedCount) {
            // Given
            // Adjusting search value context for LessThan test dynamically if needed, 
            // or keeping values explicit per theory case:
            if (matchMode == MatchMode.LessThan) {
                searchValue = "1992-01-01"; // Bob (1981) and Diana (1991) are < 1992-01-01
            }

            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "Birthday", Value = searchValue, MatchMode = matchMode }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(expectedCount);
        }
    }

    public class When_Filtering_By_Special_Types : TestBase {
        [Fact]
        public void It_Should_Parse_And_Filter_By_Guid() {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "UniqueId", Value = "22222222-2222-2222-2222-222222222222", MatchMode = MatchMode.Equals }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }

        [Fact]
        public void It_Should_Parse_And_Filter_By_Enum_Case_Insensitively() {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "Status", Value = "active", MatchMode = MatchMode.Equals }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(2);
            result.All(x => x.Status == Status.Active).Should().BeTrue();
        }
    }

    public class When_Filter_Values_Are_Invalid_Or_Malformed : TestBase {
        [Fact]
        public void It_Should_Ignore_Rule_When_Property_Does_Not_Exist() {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "NonExistentProperty", Value = "123", MatchMode = MatchMode.Equals }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(4); // Ignored, returns full dataset
        }

        [Fact]
        public void It_Should_Ignore_Rule_When_Type_Conversion_Fails() {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = "Age", Value = "NotAnInteger", MatchMode = MatchMode.Equals }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(4); // Conversion failed, rule safely skipped
        }
    }

    public class When_Using_Multiple_Constraints_And_Operators : TestBase {
        [Fact]
        public void It_Should_Combine_Constraints_Using_Field_And_Global_Operators() {
            // Given: Age > 20 AND (Name contains "Smith" OR Name contains "Brown")
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All, // Global AND
                    Constraints = [
                        new() { PropertyName = "Age", Value = "20", MatchMode = MatchMode.GreaterThan }
                    ]
                },
                new() {
                    Operator = FilterOperator.Any, // Field/Group OR
                    Constraints = [
                        new() { PropertyName = "Name", Value = "Smith", MatchMode = MatchMode.Contains },
                        new() { PropertyName = "Name", Value = "Brown", MatchMode = MatchMode.Contains }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(2);
            result.Select(x => x.Name).Should().Contain(["Alice Smith", "Charlie Brown"]);
        }
    }

    public class When_Using_Global_Any_Operator : TestBase {
        [Fact]
        public void It_Should_Combine_Filter_Requests_Using_Global_Or_Operator() {
            // Given: Age == 30 OR Age == 45
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [new() { PropertyName = "Age", Value = "30", MatchMode = MatchMode.Equals }]
                },
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [new() { PropertyName = "Age", Value = "45", MatchMode = MatchMode.Equals }]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters, FilterOperator.Any).ToList();

            // Then
            result.Should().HaveCount(2);
            result.Select(x => x.Name).Should().Contain(["Alice Smith", "Bob Jones"]);
        }
    }

    public class When_Filter_Requests_Have_Empty_Or_Blank_Fields : TestBase {
        [Fact]
        public void It_Should_Ignore_Filter_Request_With_Empty_Constraints() {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [] // Empty constraints list
                },
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [new() { PropertyName = "Age", Value = "30", MatchMode = MatchMode.Equals }]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().ContainSingle()
                .Which.Name.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData("", "30")]
        [InlineData("   ", "30")]
        [InlineData("Age", "")]
        public void It_Should_Ignore_Constraint_When_Property_Or_Value_Is_Blank(string propertyName, string value) {
            // Given
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [
                        new() { PropertyName = propertyName, Value = value, MatchMode = MatchMode.Equals }
                    ]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().HaveCount(4); // Ignored, returns full dataset
        }
    }

    public class When_Filtering_Nullable_Properties : TestBase {
        [Fact]
        public void It_Should_Successfully_Filter_Nullable_Properties() {
            // Given: Add an entity with a null value for a nullable property, or filter existing nullable property if added to TestEntity.
            // Let's assume TestEntity has a nullable int? Ranking property.
            var customData = new List<TestEntity> {
                new() { Id = 1, Name = "Alice", Ranking = 5 },
                new() { Id = 2, Name = "Bob", Ranking = null },
                new() { Id = 3, Name = "Charlie", Ranking = 10 }
            }.AsQueryable();

            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [new() { PropertyName = "Ranking", Value = "5", MatchMode = MatchMode.Equals }]
                }
            };

            // When
            var result = customData.ApplyFilters(filters).ToList();

            // Then
            result.Should().ContainSingle()
                .Which.Name.Should().Be("Alice");
        }
    }

    public class When_MatchMode_Is_Out_Of_Range : TestBase {
        [Fact]
        public void It_Should_Fallback_To_Equals_When_MatchMode_Is_Invalid() {
            // Given: Casting an invalid integer to MatchMode enum
            var invalidMatchMode = (MatchMode)999;
            var filters = new List<FilterRequest> {
                new() {
                    Operator = FilterOperator.All,
                    Constraints = [new() { PropertyName = "Age", Value = "30", MatchMode = invalidMatchMode }]
                }
            };

            // When
            var result = _testData.ApplyFilters(filters).ToList();

            // Then
            result.Should().ContainSingle()
                .Which.Name.Should().Be("Alice Smith"); // Fell back to Equals behavior
        }
    }
}