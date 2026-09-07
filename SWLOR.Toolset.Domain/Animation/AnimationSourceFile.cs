using System.Text;

namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Reads one bounded file snapshot without allocating from an unchecked file size.</summary>
public static class AnimationSourceFile
{
    public static bool Matches(string path, ReadOnlySpan<byte> expected)
    {
        using var stream = Open(path, FileOptions.SequentialScan);
        return Matches(stream, expected);
    }

    internal static bool Matches(FileStream stream, ReadOnlySpan<byte> expected)
    {
        if (stream.Length != expected.Length) return false;
        Span<byte> buffer = stackalloc byte[8192];
        while (!expected.IsEmpty)
        {
            var chunk = buffer[..Math.Min(buffer.Length, expected.Length)];
            stream.ReadExactly(chunk);
            if (!chunk.SequenceEqual(expected[..chunk.Length])) return false;
            expected = expected[chunk.Length..];
        }
        return stream.ReadByte() == -1;
    }

    public static byte[] ReadBytes(string path, int maximumBytes, string description)
    {
        using var stream = Open(path, FileOptions.SequentialScan);
        var bytes = Allocate(stream, maximumBytes, description);
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new IOException($"{description} changed while reading.");
        return bytes;
    }

    public static async Task<byte[]> ReadBytesAsync(string path, int maximumBytes, string description)
    {
        await using var stream = Open(path, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytes = Allocate(stream, maximumBytes, description);
        await stream.ReadExactlyAsync(bytes);
        if (stream.ReadByte() != -1) throw new IOException($"{description} changed while reading.");
        return bytes;
    }

    public static async Task<string> ReadTextAsync(string path, int maximumBytes, string description)
    {
        var bytes = await ReadBytesAsync(path, maximumBytes, description);
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static FileStream Open(string path, FileOptions options) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, options);

    private static byte[] Allocate(FileStream stream, int maximumBytes, string description)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        var length = stream.Length;
        if (length > maximumBytes)
            throw new InvalidDataException($"{description} exceeds {maximumBytes / (1024 * 1024)} MB.");
        return new byte[checked((int)length)];
    }
}
