namespace Dacke.Paging.Common.Cursor;

public record PagingMetadata(string DefaultSortColumn, string EntityKeyColumn, string DtoKeyColumn) { 
    public void Validate() {
        ArgumentException.ThrowIfNullOrWhiteSpace(DefaultSortColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(EntityKeyColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(DtoKeyColumn);
    }
};