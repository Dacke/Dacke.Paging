using Dacke.Paging.Common.Enums.Converters;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dacke.Paging.Common.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<MatchMode>))]
public enum MatchMode {
    [EnumMember(Value = "startsWith, sw")]
    StartsWith = 0,
    [EnumMember(Value = "contains, con")]
    Contains = 1,
    [EnumMember(Value = "notContains, notcon")]
    NotContains = 2,
    [EnumMember(Value = "endsWith, ew")]
    EndsWith = 3,
    [EnumMember(Value = "equals, eq, is")]
    Equals = 4,
    [EnumMember(Value = "notEquals, neq, isNot")]
    NotEquals = 5,
    [EnumMember(Value = "greaterThan, gt, after")]
    GreaterThan = 6,
    [EnumMember(Value = "greaterThanOrEqualTo, gte")]
    GreaterThanOrEqualTo = 7,
    [EnumMember(Value = "lessThan, lt, before")]
    LessThan = 8,
    [EnumMember(Value = "lessThanOrEqualTo, lte")]
    LessThanOrEqualTo = 9,
}