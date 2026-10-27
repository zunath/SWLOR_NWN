using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    // A structurally valid UTF-8 sequence: a lead byte and exactly the continuation bytes it needs.
    private static readonly Regex Utf8Sequence = new(
        "[\u00C2-\u00DF][\u0080-\u00BF]|[\u00E0-\u00EF][\u0080-\u00BF]{2}|" +
        "[\u00F0-\u00F4][\u0080-\u00BF]{3}",
        RegexOptions.Compiled);

    private static readonly Encoding Windows1252 = CreateWindows1252();

    // A binary GFF field as nwn_gff writes it: "type": "void" followed by its string value.
    private static readonly Regex VoidValue = new(
        @"(""type"":\s*""void"",\s*""value"":\s*)""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);

    // A JSON \u escape that is not itself escaped by a preceding backslash.
    private static readonly Regex JsonEscape = new(
        @"(?<!\\)(?:\\\\)*\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

    // Byte-mapping tables that list every Windows-1252 character on purpose, by file and member.
    private static readonly HashSet<(string File, string Member)> CharacterTables = new()
    {
        (Path.Combine("Feature", "GuiDefinition", "ExamineItemText.cs"), "highChannels"),
        (Path.Combine("Service", "ColorToken.cs"), "ColorArray")
    };

    // Existing text left as authored at the owner's request: the Disguise status label and the
    // DM window separators.
    private static readonly HashSet<(string File, string Text)> AllowedLiterals = new()
    {
        (Path.Combine("Feature", "GuiDefinition", "ViewModel", "DisguiseViewModel.cs"), "\u25CF Active"),
        (Path.Combine("Feature", "GuiDefinition", "ViewModel", "DMChooserViewModel.cs"), "Level \u00B7 HP"),
        (Path.Combine("Feature", "GuiDefinition", "ViewModel", "DMCreatorViewModel.cs"), "Level \u00B7 Faction"),
        (Path.Combine("Domain", "Administration", "DMObjectDetailsService.cs"), " \u00B7 HP "),
        (Path.Combine("Domain", "Administration", "DMObjectDetailsService.cs"), " \u00B7 ")
    };

    [Test]
    public void ModuleJsonText_UsesAsciiPunctuationAndWindows1252Letters()
    {
        var moduleRoot = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(moduleRoot, "*.json", SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(path);
            var relativePath = Path.GetRelativePath(moduleRoot, path);

            // nwn_gff writes a JSON escape as UTF-8, so an escaped non-ASCII character such as
            // \u2019 is as wrong as the character itself. Binary void values are not text.
            var source = VoidValue.Replace(Encoding.Latin1.GetString(bytes), match => match.Groups[1].Value + "\"\"");
            foreach (Match escape in JsonEscape.Matches(source))
            {
                if (Convert.ToInt32(escape.Groups[1].Value, 16) >= 0x80)
                    violations.Add($"{relativePath}: escaped U+{escape.Groups[1].Value.ToUpperInvariant()}");
            }

            if (!bytes.Any(value => value >= 0x80))
                continue;

            // Latin-1 maps each byte to one char, so raw color-token bytes stay three chars wide
            // and a multi-byte UTF-8 sequence stays visible as its separate bytes.
            using var reader = new JsonTextReader(new StringReader(Encoding.Latin1.GetString(bytes)))
            {
                DateParseHandling = DateParseHandling.None
            };
            foreach (var value in JToken.ReadFrom(reader).SelectTokens("..*").OfType<JValue>())
            {
                if (value.Type != JTokenType.String || IsVoidField(value))
                    continue;

                var text = ColorToken.Replace((string)value.Value!, string.Empty);
                // Bytes that form UTF-8 for a visible character count as UTF-8 even when each byte is
                // also a Windows-1252 letter: "ÃŠ" (C3 8A) cannot be told apart from UTF-8 "Ê", and
                // such pairs come from UTF-8 saved by mistake. A pair that would decode to an invisible
                // control character, such as C2 9A ("Âš"), is judged by the letter rule instead.
                var utf8 = Utf8Sequence.Matches(text).FirstOrDefault(match => !char.IsControl(DecodeUtf8(match.Value), 0));
                var index = utf8 != null
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

            // Roslyn decodes escapes and verbatim and raw strings, and skips comments.
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            foreach (var token in root.DescendantTokens().Where(IsStringContent))
            {
                var text = token.ValueText;
                if (AllowedLiterals.Contains((relativePath, text)))
                    continue;

                var isTable = CharacterTables.Contains((relativePath, DeclaredName(token)));
                var bad = text.FirstOrDefault(character => isTable
                    ? character >= 0x80 && !IsWindows1252(character)
                    : !IsGameTextCharacter(character));
                if (bad != default)
                {
                    var line = token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    violations.Add($"{relativePath}:{line}: U+{(int)bad:X4} in {text}");
                }
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
            // Check decoded values so JSON escapes such as \u2019 are caught too.
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            foreach (var (location, text) in StringValues(document.RootElement, "$"))
            {
                var bad = text.FirstOrDefault(character => !IsGameTextCharacter(character));
                if (bad != default)
                    violations.Add($"{Path.GetFileName(path)} {location}: U+{(int)bad:X4} in {text}");
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

    private static bool IsStringContent(SyntaxToken token) => token.Kind() is
        SyntaxKind.StringLiteralToken or SyntaxKind.CharacterLiteralToken or SyntaxKind.Utf8StringLiteralToken or
        SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken or
        SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken or
        SyntaxKind.InterpolatedStringTextToken;

    /// <summary>The field, local, or property a literal initializes, if any.</summary>
    private static string DeclaredName(SyntaxToken token)
    {
        foreach (var node in token.Parent!.AncestorsAndSelf())
        {
            if (node is VariableDeclaratorSyntax variable)
                return variable.Identifier.ValueText;
            if (node is PropertyDeclarationSyntax property)
                return property.Identifier.ValueText;
        }

        return string.Empty;
    }

    private static IEnumerable<(string Location, string Text)> StringValues(System.Text.Json.JsonElement element, string location)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                foreach (var value in StringValues(property.Value, $"{location}.{property.Name}"))
                    yield return value;
                break;
            case System.Text.Json.JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                foreach (var value in StringValues(item, $"{location}[{index++}]"))
                    yield return value;
                break;
            case System.Text.Json.JsonValueKind.String:
                yield return (location, element.GetString() ?? string.Empty);
                break;
        }
    }

    private static string DecodeUtf8(string latin1Text) =>
        Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(latin1Text));

    private static bool IsVoidField(JValue value) =>
        value.Parent is JProperty { Name: "value", Parent: JObject field } &&
        (string?)field["type"] == "void";

    /// <summary>True when a Latin-1-decoded byte is a letter in Windows-1252, such as 0xE9 (é).</summary>
    private static bool IsWindows1252Letter(char character) =>
        character <= 0xFF && char.IsLetter(Windows1252.GetString(new[] { (byte)character })[0]);

    // Game text punctuation is ASCII. Non-ASCII punctuation (curly and angled quotes, dashes, "...",
    // bullets, and the like) usually comes from text pasted out of documents and AI chats.
    private static bool IsGameTextCharacter(char character) =>
        character < 0x80 || (IsWindows1252(character) && !char.IsPunctuation(character));

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
