namespace EFCorePaging.Cursor;

public record PagingMetadata(
    string DefaultSortColumn,
    string EntityKeyColumn,
    string DtoKeyColumn);