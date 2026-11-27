using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Compares a committed text with the text a test produced, token by token (a token ends at a space or a line end), so the report names the
/// first token that differs with the line it sits on and not a raw text difference. A file that ends in ".gz" is the same text compressed
/// (a trace of megabytes of repeated calls): it is read and written through gzip, and <c>gzip -dc</c> shows it. Regenerate with
/// <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change. The text uses LF line ends whatever the platform.
/// </summary>
public static class WeaponAbilityGolden
{
    public static void Assert(string repositoryRelativePath, string actual)
    {
        var path = Path.Combine(CombatTranscriptBench.FindRepositoryRoot(), repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, Encode(path, actual));
        }

        File.Exists(path).Should().BeTrue("the snapshot is committed beside the tests");
        var committed = Decode(path);
        var expected = Tokens(committed);
        var produced = Tokens(actual);
        for (var index = 0; index < Math.Max(expected.Count, produced.Count); index++)
        {
            var left = index < expected.Count ? expected[index] : ("<end>", 0);
            var right = index < produced.Count ? produced[index] : ("<end>", 0);
            if (left.Item1 != right.Item1)
            {
                NUnit.Framework.Assert.Fail(
                    $"{repositoryRelativePath} token {index + 1} differs: expected '{left.Item1}' on line {left.Item2}, actual '{right.Item1}' on line {right.Item2}.\n" +
                    $"  expected line: {Line(committed, left.Item2)}\n  actual line:   {Line(actual, right.Item2)}");
            }
        }
    }

    private static byte[] Encode(string path, string text)
    {
        var bytes = new UTF8Encoding(false).GetBytes(text);
        if (!path.EndsWith(".gz", StringComparison.Ordinal))
            return bytes;

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
            gzip.Write(bytes);

        return output.ToArray();
    }

    private static string Decode(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (!path.EndsWith(".gz", StringComparison.Ordinal))
            return new UTF8Encoding(false).GetString(bytes);

        using var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
        using var reader = new StreamReader(input, new UTF8Encoding(false));
        return reader.ReadToEnd();
    }

    private static string Line(string text, int number)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        return number >= 1 && number <= lines.Length ? lines[number - 1] : "<end>";
    }

    private static List<(string, int)> Tokens(string text)
    {
        var tokens = new List<(string, int)>();
        var line = 1;
        foreach (var row in text.Replace("\r\n", "\n").Split('\n'))
        {
            foreach (var token in row.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                tokens.Add((token, line));
            line++;
        }

        return tokens;
    }
}
