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
/// The scan is deliberately conservative: every code line and every <c>#define</c>
/// body the mobile build might see is checked, whether or not (and in whichever
/// file) a macro is later expanded. A macro meant only for <c>#if</c> arithmetic
/// must therefore also sit behind <c>#ifndef MOBILE</c>, or the arithmetic belongs
/// on the <c>#if</c> line itself.
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

    // "#if defined(MOBILE)", "#if !defined MOBILE" and similar single-term tests.
    private static readonly Regex MobileIfExpression = new(
        @"^#\s*(el)?if\s+(?<not>!\s*)?defined\s*(\(\s*MOBILE\s*\)|MOBILE)\s*$",
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
        const string includedMacroOnly = "#define UNPACK(v) ((v >> 3) & 3)\n";
        const string guardedMacro = "#ifndef MOBILE\n#define UNPACK(v) ((v >> 3) & 3)\n#endif\nint Unpack(int v) { return 0; }\n";
        const string continuedMacro = "#define UNPACK(v) \\\n    ((v >> 3) & 3)\n";
        const string guardedContinuedMacro = "#ifndef MOBILE\n" + continuedMacro + "#endif\nint Unpack(int v) { return 0; }\n";
        const string splitOperator = "int Shift(int packed)\n{\n    return packed >\\\n> 3;\n}\n";
        const string elifAfterMobile = "#ifdef MOBILE\nint A() { return 0; }\n#elif defined(DESKTOP_FAST)\n" + unguarded + "#else\n" + unguarded + "#endif\n";

        FindMobileUnsupportedLines(unguarded).Should().ContainSingle();
        FindMobileUnsupportedLines(guarded).Should().BeEmpty();
        FindMobileUnsupportedLines(elseGuarded).Should().BeEmpty();
        FindMobileUnsupportedLines(logicalOnly).Should().BeEmpty();
        FindMobileUnsupportedLines(macro).Should().ContainSingle();
        // A definition is flagged on its own: another shader may include and expand it.
        FindMobileUnsupportedLines(includedMacroOnly).Should().ContainSingle();
        FindMobileUnsupportedLines(guardedMacro).Should().BeEmpty();
        FindMobileUnsupportedLines(continuedMacro).Should().ContainSingle();
        FindMobileUnsupportedLines(guardedContinuedMacro).Should().BeEmpty();
        FindMobileUnsupportedLines(splitOperator).Should().ContainSingle();
        FindMobileUnsupportedLines("#if !defined(MOBILE)\n" + unguarded + "#endif\n").Should().BeEmpty();
        FindMobileUnsupportedLines("#if defined MOBILE\nint A() { return 0; }\n#else\n" + unguarded + "#endif\n").Should().BeEmpty();
        FindMobileUnsupportedLines("// /*\nint B(int v) { return v & 1; }\n// */\n").Should().ContainSingle();
        FindMobileUnsupportedLines("#ifndef MOBILE\n#endif\n#define/**/UNPACK(v) ((v >> 3) & 3)\n").Should().ContainSingle();
        FindMobileUnsupportedLines("#define SAFE 1 // explanation \\\n continues with & here\nint A() { return SAFE; }\n").Should().BeEmpty();
        FindMobileUnsupportedLines(elifAfterMobile).Should().BeEmpty();
    }

    [Test]
    public void Checker_OnlyCountsDefinitelyCompiledLinesAsDefinite()
    {
        var scan = ScanForMobile("#ifdef MOBILE\nvoid A() {}\n#endif\n#if 0\nvoid B() {}\n#endif\nvoid C() {}\n#if defined(MOBILE)\nvoid D() {}\n#endif\n"
            + "#ifndef MOBILE\nvoid E() {}\n#elif defined(MOBILE)\nvoid F() {}\n#endif\n#if QUALITY\nvoid G() {}\n#elif defined(MOBILE)\nvoid H() {}\n#endif\n");

        scan.DefiniteCode.Select(line => line.Text.Trim()).Should().Equal("void A() {}", "void C() {}", "void D() {}", "void F() {}");
        scan.PossibleCode.Select(line => line.Text.Trim()).Should().Equal(
            "void A() {}", "void B() {}", "void C() {}", "void D() {}", "void F() {}", "void G() {}", "void H() {}");
    }

    private static IEnumerable<string> FindMobileUnsupportedLines(string shader)
    {
        var scan = ScanForMobile(shader);
        return scan.PossibleCode
            .Where(line => MobileUnsupportedOperator.IsMatch(line.Text))
            .Select(line => $"{line.Number}: {line.Text.Trim()}")
            .Concat(scan.Macros
                .Where(m => MobileUnsupportedOperator.IsMatch(m.Body))
                .Select(m => $"{m.Number}: #define {m.Name}{m.Body}"))
            .ToList();
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
        // Splice backslash-continued lines first, then strip comments, in the GLSL
        // preprocessor's order (the backslash and newline vanish, so "a >\" + "> 3"
        // becomes ">>", and a continued // comment swallows the next line). Spliced
        // lines leave blanks behind so line numbers stay true.
        var raw = shader.Replace("\r\n", "\n").Split('\n');
        for (var index = raw.Length - 2; index >= 0; index--)
        {
            if (raw[index].EndsWith("\\"))
            {
                raw[index] = raw[index].Substring(0, raw[index].Length - 1) + raw[index + 1];
                raw[index + 1] = string.Empty;
            }
        }

        var lines = StripComments(string.Join("\n", raw)).Split('\n');
        // Per conditional chain: Current is whether the branch now open is compiled on
        // mobile, and Matched whether an earlier branch of the chain already was
        // (null: depends on a condition unrelated to MOBILE).
        var conditions = new Stack<(bool? Current, bool? Matched)>();
        var possible = new List<(int Number, string Text)>();
        var definite = new List<(int Number, string Text)>();
        var macros = new List<(int Number, string Name, string Body)>();

        for (var index = 0; index < lines.Length; index++)
        {
            var text = lines[index];
            var directive = text.Trim();
            var possiblyActive = conditions.All(condition => condition.Current != false);

            if (directive.StartsWith("#"))
            {
                var tokens = directive.TrimStart('#').Trim().Split(new[] { ' ', '\t', '(' }, 2);
                var keyword = tokens[0];
                var argument = tokens.Length > 1 ? tokens[1].Trim().TrimEnd(')') : string.Empty;

                switch (keyword)
                {
                    case "ifdef":
                        conditions.Push(argument == "MOBILE" ? (true, true) : (null, null));
                        break;
                    case "ifndef":
                        conditions.Push(argument == "MOBILE" ? (false, false) : (null, null));
                        break;
                    case "if":
                        var mobileTest = MobileIfExpression.Match(directive);
                        if (mobileTest.Success)
                        {
                            var compiledOnMobile = mobileTest.Groups["not"].Value.Length == 0;
                            conditions.Push((compiledOnMobile, compiledOnMobile));
                        }
                        else
                        {
                            conditions.Push((null, null));
                        }
                        break;
                    case "elif":
                        if (conditions.Count > 0)
                        {
                            var chain = conditions.Pop();
                            var elifTest = MobileIfExpression.Match(directive);
                            bool? elifValue = elifTest.Success ? elifTest.Groups["not"].Value.Length == 0 : null;
                            if (chain.Matched == true)
                            {
                                // A branch that already matched skips every later #elif.
                                conditions.Push((false, true));
                            }
                            else if (elifValue.HasValue && chain.Matched == false)
                            {
                                conditions.Push((elifValue, elifValue));
                            }
                            else if (elifValue == false)
                            {
                                conditions.Push((false, chain.Matched));
                            }
                            else
                            {
                                // An earlier unknown branch may have matched, or the test is unknown.
                                conditions.Push((null, elifValue == true ? true : null));
                            }
                        }
                        break;
                    case "else":
                        if (conditions.Count > 0)
                        {
                            var chain = conditions.Pop();
                            conditions.Push((chain.Matched.HasValue ? !chain.Matched.Value : null, true));
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
            if (conditions.All(condition => condition.Current == true))
            {
                definite.Add((index + 1, text));
            }
        }

        return new MobileScan(possible, definite, macros);
    }

    private static string StripComments(string shader)
    {
        // One lexical pass, so "/*" inside a line comment (or "//" inside a block
        // comment) stays inert. A block comment counts as whitespace ("#define/**/X"
        // is "#define X") and keeps its newlines so line numbers stay true.
        var result = new System.Text.StringBuilder(shader.Length);
        var index = 0;
        while (index < shader.Length)
        {
            if (shader[index] == '/' && index + 1 < shader.Length && shader[index + 1] == '/')
            {
                while (index < shader.Length && shader[index] != '\n')
                {
                    index++;
                }
            }
            else if (shader[index] == '/' && index + 1 < shader.Length && shader[index + 1] == '*')
            {
                var end = shader.IndexOf("*/", index + 2, System.StringComparison.Ordinal);
                var stop = end < 0 ? shader.Length : end + 2;
                result.Append(' ').Append('\n', shader.Substring(index, stop - index).Count(c => c == '\n'));
                index = stop;
            }
            else
            {
                result.Append(shader[index]);
                index++;
            }
        }

        return result.ToString();
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
