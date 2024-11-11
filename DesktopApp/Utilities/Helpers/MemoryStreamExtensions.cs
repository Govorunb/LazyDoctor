namespace DesktopApp.Utilities.Helpers;

internal static class MemoryStreamExtensions
{
    public static ReadOnlySpan<byte> AsSpan(this MemoryStream stream)
        => stream.TryGetBuffer(out var buf) ? buf
            : stream.UnsafeGetBuffer();

    private static ArraySegment<byte> UnsafeGetBuffer(this MemoryStream stream)
        => new(GetBuffer(stream), 0, (int)stream.Length);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_buffer")]
    private static extern byte[] GetBuffer(this MemoryStream stream);
}
