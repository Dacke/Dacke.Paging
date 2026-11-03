using AwesomeAssertions;
using Dacke.EFCorePaging.Cursor.Helpers;
using System.Text;

namespace Dacke.Paging.UnitTests.Helpers;

public class CursorHelperTests {
    public class TestCursorPayload {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class When_Encoding_Cursor {
        [Fact]
        public void It_Should_Return_Base64_Encoded_Json_String_When_Data_Is_Valid() {
            // Given
            var payload = new TestCursorPayload { Id = 42, Name = "Alice" };

            // When
            var encoded = CursorHelper.EncodeCursor(payload);

            // Then
            encoded.Should().NotBeNullOrWhiteSpace();

            // Verify roundtrip payload structure
            var decoded = CursorHelper.DecodeCursor<TestCursorPayload>(encoded);
            decoded.Should().NotBeNull();
            decoded!.Id.Should().Be(42);
            decoded.Name.Should().Be("Alice");
        }

        [Fact]
        public void It_Should_Return_Empty_String_When_Data_IsNull() {
            // Given
            TestCursorPayload? payload = null;

            // When
            var encoded = CursorHelper.EncodeCursor(payload);

            // Then
            encoded.Should().Be(string.Empty);
        }
    }

    public class When_Decoding_Cursor {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void It_Should_Return_Default_When_Cursor_String_Is_Null_Or_Whitespace(string? cursor) {
            // Given
            // (Provided via InlineData)

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(cursor);

            // Then
            result.Should().BeNull();
        }

        [Fact]
        public void It_Should_Successfully_Decode_Valid_Base64_Cursor() {
            // Given
            var payload = new TestCursorPayload { Id = 100, Name = "Bob" };
            var encoded = CursorHelper.EncodeCursor(payload);

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(encoded);

            // Then
            result.Should().NotBeNull();
            result!.Id.Should().Be(100);
            result.Name.Should().Be("Bob");
        }

        [Fact]
        public void It_Should_Return_Default_When_Base64_String_Is_Malformed() {
            // Given
            var invalidBase64 = "Not-A-Valid-Base64-String!@#";

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(invalidBase64);

            // Then
            result.Should().BeNull();
        }

        [Fact]
        public void It_Should_Return_Default_When_Json_Deserialization_Fails() {
            // Given: Valid Base64 string containing malformed JSON syntax
            var malformedJson = "{ unclosed json syntax";
            var encodedMalformed = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(malformedJson));

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(encodedMalformed);

            // Then
            result.Should().BeNull();
        }
    }

    public class When_Decoding_Cursor_With_Different_Casing {
        [Fact]
        public void It_Should_Deserialize_Properties_Case_Insensitively() {
            // Given: Encoded JSON with lowercase property names instead of PascalCase
            var json = "{\"id\": 7, \"name\": \"Diana\"}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(encoded);

            // Then
            result.Should().NotBeNull();
            result!.Id.Should().Be(7);
            result.Name.Should().Be("Diana");
        }
    }

    public class When_Using_Struct_Payloads : CursorHelperTests {
        public struct TestStructPayload {
            public int Value { get; set; }
        }

        [Fact]
        public void It_Should_Return_Default_Struct_When_Cursor_Is_Invalid() {
            // Given
            string? invalidCursor = null;

            // When
            var result = CursorHelper.DecodeCursor<TestStructPayload>(invalidCursor);

            // Then
            result.Should().Be(default(TestStructPayload)); // Value type default (Value = 0)
        }
    }

    public class When_Decoding_Empty_Json_Payload : CursorHelperTests {
        [Fact]
        public void It_Should_Deserialize_Empty_Json_Object_Successfully() {
            // Given: Encoded empty json object "{}"
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));

            // When
            var result = CursorHelper.DecodeCursor<TestCursorPayload>(encoded);

            // Then
            result.Should().NotBeNull();
            result!.Id.Should().Be(0); // Default int value
            result.Name.Should().BeNullOrEmpty();
        }
    }
}