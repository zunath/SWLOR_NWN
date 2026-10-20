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

    // "#define NAME body" or "#define NAME(args) body"; captures the replacement text.
    private static readonly Regex MacroReplacement = new(
        @"^#\s*define\s+\w+(\([^)]*\))?(?<body>.*)$",
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
        var mobileLines = MobileCompiledLines(shader).Select(line => line.Text.Trim()).ToList();

        mobileLines.Should().Contain("gl_FragColor = FS_PostProcess();");
        mobileLines.Should().NotContain(line => line.Contains("Telegraph"),
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
        const string guardedMacro = "#ifndef MOBILE\n#define UNPACK(v) ((v >> 3) & 3)\n#endif\n";

        FindMobileUnsupportedLines(unguarded).Should().ContainSingle();
        FindMobileUnsupportedLines(guarded).Should().BeEmpty();
        FindMobileUnsupportedLines(elseGuarded).Should().BeEmpty();
        FindMobileUnsupportedLines(logicalOnly).Should().BeEmpty();
        FindMobileUnsupportedLines(macro).Should().ContainSingle();
        FindMobileUnsupportedLines(guardedMacro).Should().BeEmpty();
    }

    private static IEnumerable<string> FindMobileUnsupportedLines(string shader)
    {
        return MobileCompiledLines(shader)
            .Where(line => MobileUnsupportedOperator.IsMatch(line.Text))
            .Select(line => $"{line.Number}: {line.Text.Trim()}");
    }

    /// <summary>
    /// Returns the non-preprocessor code lines (comments removed) that are compiled
    /// when MOBILE is defined. Conditions that do not test MOBILE are treated as
    /// possibly true, so their contents are checked.
    /// </summary>
    private static IEnumerable<(int Number, string Text)> MobileCompiledLines(string shader)
    {
        var lines = StripComments(shader).Replace("\r\n", "\n").Split('\n');
        // null: condition unrelated to MOBILE; true/false: branch compiled on mobile.
        var conditions = new Stack<bool?>();

        for (var index = 0; index < lines.Length; index++)
        {
            var text = lines[index];
            var directive = text.Trim();

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
                        // Macro bodies are expanded into the code that uses them, so an
                        // active definition's replacement text is checked like code.
                        var replacement = MacroReplacement.Match(directive);
                        if (replacement.Success
                            && conditions.All(condition => condition != false)
                            && replacement.Groups["body"].Value.Trim().Length > 0)
                        {
                            yield return (index + 1, replacement.Groups["body"].Value);
                        }
                        break;
                }

                continue;
            }

            if (conditions.All(condition => condition != false) && directive.Length > 0)
            {
                yield return (index + 1, text);
            }
        }
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
