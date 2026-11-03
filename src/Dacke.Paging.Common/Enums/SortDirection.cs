using Dacke.Paging.Common.Enums.Converters;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dacke.Paging.Common.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<SortDirection>))]
public enum SortDirection {
    [EnumMember(Value = "ascending, asc")]
    Ascending = 0,
    [EnumMember(Value = "descending, desc")]
    Descending = 1,
}