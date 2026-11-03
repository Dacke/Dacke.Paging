using Paging.Common.Enums.Converters;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Paging.Common.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<FilterOperator>))]
public enum FilterOperator {
    [EnumMember(Value = "all, and")]
    All = 0,
    [EnumMember(Value = "any, or")]
    Any = 1
}