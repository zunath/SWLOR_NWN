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

    // UTF-8 encodes every non-ASCII character as a lead byte followed by continuation bytes.
    private static readonly Regex Utf8Sequence = new("[\u00C2-\u00F4][\u0080-\u00BF]+", RegexOptions.Compiled);

    private static readonly Encoding Windows1252 = CreateWindows1252();

    // Typographic punctuation pasted from documents and AI chats. The game can display these, but
    // game text uses the ASCII forms (', ", -, ...) so every source follows one rule.
    private const string TypographicPunctuation = "‘’“”–—…";

    // Byte-mapping tables that list every Windows-1252 character on purpose.
    private static readonly HashSet<string> CharacterTableFiles = new(StringComparer.Ordinal)
    {
        Path.Combine("Feature", "GuiDefinition", "ExamineItemText.cs"),
        Path.Combine("Service", "ColorToken.cs")
    };

    // Left as authored at the owner's request.
    private static readonly HashSet<string> AllowedCodeLines = new(StringComparer.Ordinal)
    {
        "StatusText = \"● Active\";"
    };

    [Test]
    public void ModuleJsonText_UsesAsciiPunctuationAndWindows1252Letters()
    {
        var moduleRoot = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(moduleRoot, "*.json", SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(path);
            if (!bytes.Any(value => value >= 0x80))
                continue;

            // Latin-1 maps each byte to one char, so raw color-token bytes stay three chars wide
            // and a multi-byte UTF-8 sequence stays visible as its separate bytes.
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
                var utf8 = Utf8Sequence.Match(text);
                var index = utf8.Success
                    ? utf8.Index
                    : text.ToList().FindIndex(character => character >= 0x80 && !IsWindows1252Letter(character));
                if (index >= 0)
                    violations.Add($"{relativePath} {value.Path}: ...{Snippet(text, index)}...");
            }
        }

        violations.Should().BeEmpty(
            "Module JSON punctuation must be ASCII (straight quotes, '-', '...') and accented letters " +
            "must be single Windows-1252 bytes, never UTF-8");
    }

    [Test]
    public void GameServerCode_UsesWindows1252AndAsciiPunctuation()
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
                if (AllowedCodeLines.Contains(code.Trim()))
                    continue;

                var isTable = CharacterTableFiles.Contains(relativePath);
                var bad = code.FirstOrDefault(character => isTable
                    ? character >= 0x80 && !IsWindows1252(character)
                    : !IsGameTextCharacter(character));
                if (bad != default)
                    violations.Add($"{relativePath}:{lineIndex + 1}: U+{(int)bad:X4} in {code.Trim()}");
            }
        }

        violations.Should().BeEmpty(
            "strings reach the game as Windows-1252, so other characters display as '?', and game text " +
            "uses ASCII quotes, dashes, and '...'");
    }

    [Test]
    public void ConversationData_UsesWindows1252AndAsciiPunctuation()
    {
        var dataRoot = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "ConversationData");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(dataRoot, "*.json", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(path);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var bad = lines[lineIndex].FirstOrDefault(character => !IsGameTextCharacter(character));
                if (bad != default)
                    violations.Add($"{Path.GetFileName(path)}:{lineIndex + 1}: U+{(int)bad:X4} in {lines[lineIndex].Trim()}");
            }
        }

        violations.Should().BeEmpty(
            "conversation text reaches the game as Windows-1252, so other characters display as '?', and " +
            "game text uses ASCII quotes, dashes, and '...'");
    }

    [Test]
    public void CustomTlkText_UsesWindows1252AndAsciiPunctuation()
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));

        var violations = document.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => (Id: entry.GetProperty("id").GetInt32(), Text: entry.GetProperty("text").GetString() ?? string.Empty))
            .Where(entry => !entry.Text.All(IsGameTextCharacter))
            .Select(entry => $"{entry.Id}: {entry.Text}")
            .ToList();

        violations.Should().BeEmpty(
            "nwn_tlk converts TLK text to Windows-1252, and game text uses ASCII quotes, dashes, and '...'");
    }

    private static bool IsVoidField(JValue value) =>
        value.Parent is JProperty { Name: "value", Parent: JObject field } &&
        (string?)field["type"] == "void";

    /// <summary>True when a Latin-1-decoded byte is a letter in Windows-1252, such as 0xE9 (é).</summary>
    private static bool IsWindows1252Letter(char character) =>
        character <= 0xFF && char.IsLetter(Windows1252.GetString(new[] { (byte)character })[0]);

    private static bool IsGameTextCharacter(char character) =>
        character < 0x80 || (IsWindows1252(character) && !TypographicPunctuation.Contains(character));

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
