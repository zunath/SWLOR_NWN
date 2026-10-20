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
/// post-processing pass) blanked the whole 3D view on mobile.
///
/// Rule for SWLOR's custom shaders: code that uses these operators sits inside an
/// <c>#ifndef MOBILE</c> block or the <c>#else</c> of an <c>#ifdef MOBILE</c> block.
/// This test checks the shaders as they are written in that style; it is not a
/// general GLSL preprocessor, so other conditions are treated as possibly compiled.
/// </summary>
public class CustomShaderMobileCompatibilityTests
{
    // Bitwise and modulo operators, including compound assignments. Logical
    // && / || / ^^ are valid GLSL ES 1.00 and are not matched.
    private static readonly Regex MobileUnsupportedOperator = new(
        @"<<|>>|(?<!&)&(?!&)|(?<!\|)\|(?!\|)|(?<!\^)\^(?!\^)|%|~",
        RegexOptions.Compiled);

    [Test]
    public void CustomShaders_KeepBitwiseOperatorsOutOfMobileCode()
    {
        var shaders = Directory.GetFiles(ResolveRepositoryPath("SWLOR_Haks", "sw_shader"), "*.shd");
        shaders.Should().NotBeEmpty("the hak ships custom shaders");

        var findings = shaders
            .SelectMany(path => MobileLines(File.ReadAllText(path))
                .Where(line => !line.Text.TrimStart().StartsWith("#if")
                               && MobileUnsupportedOperator.IsMatch(line.Text))
                .Select(line => $"{Path.GetFileName(path)}:{line.Number}: {line.Text.Trim()}"))
            .ToList();

        findings.Should().BeEmpty(
            "GLSL ES 1.00 rejects these operators, so the shader fails to compile on mobile; " +
            "put desktop-only code behind #ifndef MOBILE");
    }

    [Test]
    public void PostProcessShader_RunsTheStockPassOnMobile()
    {
        var shader = File.ReadAllText(ResolveRepositoryPath("SWLOR_Haks", "sw_shader", "fsfbpostpr.shd"));
        var mobileCode = MobileLines(shader).Select(line => line.Text.Trim()).ToList();

        mobileCode.Should().Contain("gl_FragColor = FS_PostProcess();");
        mobileCode.Should().NotContain(line => line.Contains("Telegraph"),
            "telegraph unpacking needs integer bitwise operators that mobile clients lack");
    }

    [Test]
    public void MobileLines_SkipDesktopOnlyBlocks()
    {
        const string unpack = "int Unpack(int v) { return (v >> 3) & 3; }\n";

        BitwiseMobileLines(unpack).Should().ContainSingle();
        BitwiseMobileLines("#ifndef MOBILE\n" + unpack + "#endif\n").Should().BeEmpty();
        BitwiseMobileLines("#ifdef MOBILE\nint A() { return 0; }\n#else\n" + unpack + "#endif\n").Should().BeEmpty();
        BitwiseMobileLines("#ifdef MOBILE\n" + unpack + "#endif\n").Should().ContainSingle();
        BitwiseMobileLines("#if QUALITY\n" + unpack + "#endif\n").Should().ContainSingle();
        BitwiseMobileLines("// " + unpack + "/* " + unpack + "*/\n").Should().BeEmpty();
        BitwiseMobileLines("bool Both(bool a, bool b) { return a && b || a ^^ b; }\n").Should().BeEmpty();
    }

    private static List<(int Number, string Text)> BitwiseMobileLines(string shader)
    {
        return MobileLines(shader).Where(line => MobileUnsupportedOperator.IsMatch(line.Text)).ToList();
    }

    /// <summary>
    /// Lines (comments removed) outside desktop-only blocks. Only #ifdef/#ifndef MOBILE
    /// decide; every other #if/#ifdef/#ifndef is treated as possibly compiled.
    /// </summary>
    private static IEnumerable<(int Number, string Text)> MobileLines(string shader)
    {
        var lines = StripComments(shader).Replace("\r\n", "\n").Split('\n');
        // true: compiled on mobile; false: desktop only; null: unrelated to MOBILE.
        var blocks = new Stack<bool?>();

        for (var index = 0; index < lines.Length; index++)
        {
            var directive = lines[index].Trim();
            if (directive.StartsWith("#ifdef") || directive.StartsWith("#ifndef") || directive.StartsWith("#if"))
            {
                var isMobile = Regex.IsMatch(directive, @"^#if(n?)def\s+MOBILE\b");
                blocks.Push(isMobile ? !directive.StartsWith("#ifndef") : null);
            }
            else if (directive.StartsWith("#else") || directive.StartsWith("#elif"))
            {
                if (blocks.Count > 0)
                {
                    var current = blocks.Pop();
                    blocks.Push(directive.StartsWith("#else") && current.HasValue ? !current.Value : null);
                }
            }
            else if (directive.StartsWith("#endif"))
            {
                if (blocks.Count > 0)
                {
                    blocks.Pop();
                }
            }
            else if (directive.Length > 0 && blocks.All(block => block != false))
            {
                yield return (index + 1, lines[index]);
            }
        }
    }

    private static string StripComments(string shader)
    {
        var withoutBlocks = Regex.Replace(shader, @"/\*.*?\*/",
            match => " " + new string('\n', match.Value.Count(c => c == '\n')), RegexOptions.Singleline);
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
