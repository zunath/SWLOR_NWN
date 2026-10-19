using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The game reads text as Windows-1252. nwn_gff copies Module JSON string bytes into the module
/// verbatim, so a UTF-8 character such as a curly apostrophe shows in game as "â€™". These tests
/// keep game text in an encoding the client displays (see Readmes/CodingStandards.md).
/// </summary>
public class GameTextEncodingTests
{
    // An NWN color token is "<c" followed by three raw channel bytes and ">".
    private static readonly Regex ColorToken = new("<c[\\s\\S]{3}>", RegexOptions.Compiled);

    private static readonly Encoding Windows1252 = CreateWindows1252();

    [Test]
    public void ModuleJsonText_IsAsciiOutsideColorTokens()
    {
        var moduleRoot = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(moduleRoot, "*.json", SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(path);
            if (!bytes.Any(value => value >= 0x80))
                continue;

            // Latin-1 maps each byte to one char, so raw color-token bytes stay three chars wide
            // and a multi-byte UTF-8 sequence shows up as non-ASCII text outside any token.
            using var reader = new JsonTextReader(new StringReader(Encoding.Latin1.GetString(bytes)))
            {
                DateParseHandling = DateParseHandling.None
            };
            var relativePath = Path.GetRelativePath(moduleRoot, path);
            foreach (var value in JToken.ReadFrom(reader).SelectTokens("..*").OfType<JValue>())
            {
                if (value.Type != JTokenType.String || IsVoidField(value))
                    continue;

                var text = ColorToken.Replace((string)value.Value!, string.Empty);
                var index = text.ToList().FindIndex(character => character >= 0x80);
                if (index >= 0)
                    violations.Add($"{relativePath} {value.Path}: ...{Snippet(text, index)}...");
            }
        }

        violations.Should().BeEmpty(
            "Module JSON text must be plain ASCII (straight quotes, '-', '...'); only color-token " +
            "channels may hold raw Windows-1252 bytes");
    }

    [Test]
    public void GameServerCode_HasNoCharactersOutsideWindows1252()
    {
        var sourceRoot = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, path);
            if (relativePath.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                relativePath.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue;

            var lines = File.ReadAllLines(path);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var code = StripComment(lines[lineIndex]);
                var bad = code.FirstOrDefault(character => character >= 0x80 && !IsWindows1252(character));
                if (bad != default)
                    violations.Add($"{relativePath}:{lineIndex + 1}: U+{(int)bad:X4} in {code.Trim()}");
            }
        }

        violations.Should().BeEmpty(
            "strings reach the game as Windows-1252, so other characters display as '?'");
    }

    [Test]
    public void CustomTlkText_HasNoCharactersOutsideWindows1252()
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));

        var violations = document.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => (Id: entry.GetProperty("id").GetInt32(), Text: entry.GetProperty("text").GetString() ?? string.Empty))
            .Where(entry => entry.Text.Any(character => character >= 0x80 && !IsWindows1252(character)))
            .Select(entry => $"{entry.Id}: {entry.Text}")
            .ToList();

        violations.Should().BeEmpty("nwn_tlk converts TLK text to Windows-1252");
    }

    private static bool IsVoidField(JValue value) =>
        value.Parent is JProperty { Name: "value", Parent: JObject field } &&
        (string?)field["type"] == "void";

    private static bool IsWindows1252(char character)
    {
        try
        {
            Windows1252.GetBytes(new[] { character });
            return true;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>Drops a trailing // comment that is not inside a string literal.</summary>
    private static string StripComment(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*') ||
            trimmed.StartsWith("/*", StringComparison.Ordinal))
            return string.Empty;

        var inString = false;
        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] == '\\' && inString)
                index++;
            else if (line[index] == '"')
                inString = !inString;
            else if (!inString && line[index] == '/' && index + 1 < line.Length && line[index + 1] == '/')
                return line[..index];
        }

        return line;
    }

    private static string Snippet(string text, int index)
    {
        var start = Math.Max(0, index - 30);
        return text.Substring(start, Math.Min(text.Length - start, 50));
    }

    private static Encoding CreateWindows1252()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Module", "ifo", "module.ifo.json")) ||
                Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR repository root.");
    }
}
