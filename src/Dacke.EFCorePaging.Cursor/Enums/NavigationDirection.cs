using Dacke.Paging.Common.Enums.Converters;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Dacke.EFCorePaging.Cursor.Enums;

[JsonConverter(typeof(EnumMemberJsonConverter<NavigationDirection>))]
public enum NavigationDirection {
    [EnumMember(Value = "forward")]
    Forward = 0,
    [EnumMember(Value = "backward")]
    Backward = 1
}
