namespace Dacke.EFCorePaging.Cursor;

public class KeysetCursor {
    public Dictionary<string, string?> SortValues { get; set; } = new();
    public string PrimaryKeyName { get; set; } = string.Empty;
    public string PrimaryKeyValue { get; set; } = string.Empty;

    public KeysetCursor() { }   //  Do not remove.  Needed for serialization

    public KeysetCursor(string primaryKeyName, object primaryKeyValue, IEnumerable<(string PropertyName, object? Value)> sortValues) {
        PrimaryKeyName = primaryKeyName;
        PrimaryKeyValue = primaryKeyValue?.ToString() ?? string.Empty;

        foreach (var (prop, val) in sortValues)
            SortValues[prop] = val?.ToString();
    }
}
