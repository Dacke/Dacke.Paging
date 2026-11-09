namespace Dacke.Paging.Sample.Web.Tests.Extensions;

public static class IntegerExtensions {
    public static Guid AsGuid(this int value) {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0); // put int in first 4 bytes
        return new Guid(bytes);
    }
}