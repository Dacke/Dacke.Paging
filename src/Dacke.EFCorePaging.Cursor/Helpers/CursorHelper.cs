using System.Text;
using System.Text.Json;

namespace Dacke.EFCorePaging.Cursor.Helpers;

public static class CursorHelper {
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Serializes and Base64 encodes an object into an opaque cursor string.
    /// </summary>
    public static string EncodeCursor<T>(T cursorData) {
        if (cursorData == null)
            return string.Empty;

        var json = JsonSerializer.Serialize(cursorData, JsonOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Decodes a Base64 string back into the strongly-typed cursor payload.
    /// Returns default/null if the cursor string is null, empty, or invalid.
    /// </summary>
    public static T? DecodeCursor<T>(string? cursor) {
        if (string.IsNullOrWhiteSpace(cursor))
            return default;

        try {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch {
            return default;
        }
    }
}