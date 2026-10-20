using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The NWN:EE mobile client compiles shaders as GLSL ES 1.00, which has no integer
/// bitwise or modulo operators. One failing override (fsfbpostpr, the full-screen
/// post-processing pass) blanked the whole 3D view on mobile. Desktop-only code
/// must sit behind <c>#ifndef MOBILE</c> (or the <c>#else</c> of <c>#ifdef MOBILE</c>).
/// </summary>
public class CustomShaderMobileCompatibilityTests
{
    // Runtime bitwise and modulo operators, including compound assignments.
    // Logical && / || / ^^ are valid GLSL ES 1.00 and are not matched.
    private static readonly Regex MobileUnsupportedOperator = new(
        @"<<|>>|(?<!&)&(?!&)|(?<!\|)\|(?!\|)|(?<!\^)\^(?!\^)|%|~",
        RegexOptions.Compiled);

    // "#define NAME body" or "#define NAME(args) body"; captures the name and replacement text.
    private static readonly Regex MacroDefinition = new(
        @"^#\s*define\s+(?<name>\w+)(\([^)]*\))?(?<body>.*)$",
        RegexOptions.Compiled);

    [Test]
    public void CustomShaders_UseNoBitwiseOperatorsInCodeTheMobileClientCompiles()
    {
        var shaderDirectory = ResolveRepositoryPath("SWLOR_Haks", "sw_shader");
        var shaders = Directory.GetFiles(shaderDirectory, "*.shd");
        shaders.Should().NotBeEmpty("the hak ships custom shaders");

        var findings = shaders
            .SelectMany(path => FindMobileUnsupportedLines(File.ReadAllText(path))
                .Select(finding => $"{Path.GetFileName(path)}:{finding}"))
            .ToList();

        findings.Should().BeEmpty(
            "GLSL ES 1.00 rejects these operators, so the shader fails to compile on mobile; " +
            "guard desktop-only code with #ifndef MOBILE");
    }

    [Test]
    public void PostProcessShader_UsesTheStockPassOnMobile()
    {
        var shader = File.ReadAllText(ResolveRepositoryPath("SWLOR_Haks", "sw_shader", "fsfbpostpr.shd"));
        var scan = ScanForMobile(shader);

        scan.DefiniteCode.Select(line => line.Text.Trim()).Should().Contain("gl_FragColor = FS_PostProcess();",
            "every mobile build must run the stock post-processing pass");
        scan.PossibleCode.Select(line => line.Text).Should().NotContain(line => line.Contains("Telegraph"),
            "telegraph unpacking needs integer bitwise operators that mobile clients lack");
    }

    [Test]
    public void Checker_FlagsUnguardedBitwiseUnpacking()
    {
        const string unguarded = "int Unpack(int packedData)\n{\n    return (packedData >> 3) & 0x3;\n}\n";
        const string guarded = "#ifndef MOBILE\n" + unguarded + "#endif\n";
        const string elseGuarded = "#ifdef MOBILE\nint Unpack(int packedData) { return 0; }\n#else\n" + unguarded + "#endif\n";
        const string logicalOnly = "bool Both(bool a, bool b) { return a && b || a ^^ b; }\n#if (FLAGS & 2)\n#endif\n#define SHADER_TYPE 2\n";
        const string macro = "#define UNPACK(v) ((v >> 3) & 3)\nint Unpack(int v) { return UNPACK(v); }\n";
        const string nestedMacro = "#define LOW(v) (v & 3)\n#define UNPACK(v) LOW(v)\nint Unpack(int v) { return UNPACK(v); }\n";
        const string guardedMacro = "#ifndef MOBILE\n#define UNPACK(v) ((v >> 3) & 3)\n#endif\nint Unpack(int v) { return 0; }\n";
        const string preprocessorOnlyMacro = "#define HAS_FLAG (FLAGS & 2)\n#if HAS_FLAG\n#endif\n";
        const string unusedMacro = "#define UNUSED(v) (v & 1)\n";

        FindMobileUnsupportedLines(unguarded).Should().ContainSingle();
        FindMobileUnsupportedLines(guarded).Should().BeEmpty();
        FindMobileUnsupportedLines(elseGuarded).Should().BeEmpty();
        FindMobileUnsupportedLines(logicalOnly).Should().BeEmpty();
        FindMobileUnsupportedLines(macro).Should().ContainSingle();
        FindMobileUnsupportedLines(nestedMacro).Should().ContainSingle();
        FindMobileUnsupportedLines(guardedMacro).Should().BeEmpty();
        FindMobileUnsupportedLines(preprocessorOnlyMacro).Should().BeEmpty();
        FindMobileUnsupportedLines(unusedMacro).Should().BeEmpty();
    }

    [Test]
    public void Checker_OnlyCountsDefinitelyCompiledLinesAsDefinite()
    {
        var scan = ScanForMobile("#ifdef MOBILE\nvoid A() {}\n#endif\n#if 0\nvoid B() {}\n#endif\nvoid C() {}\n");

        scan.DefiniteCode.Select(line => line.Text.Trim()).Should().Equal("void A() {}", "void C() {}");
        scan.PossibleCode.Select(line => line.Text.Trim()).Should().Equal("void A() {}", "void B() {}", "void C() {}");
    }

    private static IEnumerable<string> FindMobileUnsupportedLines(string shader)
    {
        var scan = ScanForMobile(shader);
        var findings = scan.PossibleCode
            .Where(line => MobileUnsupportedOperator.IsMatch(line.Text))
            .Select(line => $"{line.Number}: {line.Text.Trim()}")
            .ToList();

        // A macro body only reaches the GLSL compiler when runtime code expands it;
        // macros used only by #if are evaluated by the preprocessor, which allows
        // these operators. Follow macro-to-macro references to a fixed point.
        var used = new HashSet<string>();
        var pending = new Queue<string>(scan.PossibleCode.Select(line => line.Text));
        while (pending.Count > 0)
        {
            var text = pending.Dequeue();
            foreach (var definition in scan.Macros.Where(m => !used.Contains(m.Name) && Regex.IsMatch(text, $@"\b{m.Name}\b")))
            {
                used.Add(definition.Name);
                pending.Enqueue(definition.Body);
            }
        }

        findings.AddRange(scan.Macros
            .Where(m => used.Contains(m.Name) && MobileUnsupportedOperator.IsMatch(m.Body))
            .Select(m => $"{m.Number}: #define {m.Name}{m.Body}"));

        return findings;
    }

    private sealed record MobileScan(
        List<(int Number, string Text)> PossibleCode,
        List<(int Number, string Text)> DefiniteCode,
        List<(int Number, string Name, string Body)> Macros);

    /// <summary>
    /// Splits a shader (comments removed) into the code lines and macro definitions the
    /// mobile client may compile. Conditions that do not test MOBILE are unknown: their
    /// contents count as possibly compiled, but only lines under no condition or a
    /// MOBILE-true one count as definitely compiled.
    /// </summary>
    private static MobileScan ScanForMobile(string shader)
    {
        var lines = StripComments(shader).Replace("\r\n", "\n").Split('\n');
        // null: condition unrelated to MOBILE; true/false: branch compiled on mobile.
        var conditions = new Stack<bool?>();
        var possible = new List<(int Number, string Text)>();
        var definite = new List<(int Number, string Text)>();
        var macros = new List<(int Number, string Name, string Body)>();

        for (var index = 0; index < lines.Length; index++)
        {
            var text = lines[index];
            var directive = text.Trim();
            var possiblyActive = conditions.All(condition => condition != false);

            if (directive.StartsWith("#"))
            {
                var tokens = directive.TrimStart('#').Trim().Split(new[] { ' ', '\t', '(' }, 2);
                var keyword = tokens[0];
                var argument = tokens.Length > 1 ? tokens[1].Trim().TrimEnd(')') : string.Empty;

                switch (keyword)
                {
                    case "ifdef":
                        conditions.Push(argument == "MOBILE" ? true : null);
                        break;
                    case "ifndef":
                        conditions.Push(argument == "MOBILE" ? false : null);
                        break;
                    case "if":
                        conditions.Push(null);
                        break;
                    case "elif":
                        if (conditions.Count > 0) { conditions.Pop(); }
                        conditions.Push(null);
                        break;
                    case "else":
                        if (conditions.Count > 0)
                        {
                            var current = conditions.Pop();
                            conditions.Push(current.HasValue ? !current.Value : null);
                        }
                        break;
                    case "endif":
                        if (conditions.Count > 0) { conditions.Pop(); }
                        break;
                    case "define":
                        var definition = MacroDefinition.Match(directive);
                        if (definition.Success && possiblyActive)
                        {
                            macros.Add((index + 1, definition.Groups["name"].Value, definition.Groups["body"].Value));
                        }
                        break;
                }

                continue;
            }

            if (directive.Length == 0 || !possiblyActive)
            {
                continue;
            }

            possible.Add((index + 1, text));
            if (conditions.All(condition => condition == true))
            {
                definite.Add((index + 1, text));
            }
        }

        return new MobileScan(possible, definite, macros);
    }

    private static string StripComments(string shader)
    {
        var withoutBlocks = Regex.Replace(shader, @"/\*.*?\*/", match => new string('\n', match.Value.Count(c => c == '\n')), RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\r\n]*", string.Empty);
    }

    private static string ResolveRepositoryPath(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root must be locatable from the test directory");

        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }
}
