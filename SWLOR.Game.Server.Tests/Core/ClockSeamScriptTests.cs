using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Runs <c>tools/Refactor/Apply-ClockSeam.ps1</c> on a throwaway copy of a few sample files: the rewrite is exact, keeps
/// each file's encoding and line endings, and a second run changes nothing.
/// </summary>
[TestFixture]
public class ClockSeamScriptTests
{
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    // A non-ASCII letter, so the sample proves the script keeps multi-byte characters byte for byte.
    private static readonly string EAcute = ((char)0xE9).ToString();

    private string _temp = null!;

    [SetUp]
    public void CreateSandbox()
    {
        _temp = Path.Combine(Path.GetTempPath(), "clock-seam-" + Guid.NewGuid().ToString("N"));
        var refactor = Path.Combine(_temp, "tools", "Refactor");
        Directory.CreateDirectory(refactor);
        var root = ClockSeamSources.FindRepositoryRoot();
        File.Copy(Path.Combine(root, "tools", "Refactor", "Apply-ClockSeam.ps1"), Path.Combine(refactor, "Apply-ClockSeam.ps1"));
        File.WriteAllText(Path.Combine(refactor, "ClockSeamFiles.txt"),
            "# sample manifest\nService/WithUsings.cs\nService/Folder/**\n");
        Directory.CreateDirectory(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "Folder"));
    }

    [TearDown]
    public void DeleteSandbox()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    [Test]
    public void RewritesTheSamplesExactlyAndASecondRunChangesNothing()
    {
        var withUsings = Write("Service/WithUsings.cs", Utf8WithoutBom, Sample("\r\n", rewritten: false, coreUsing: false));
        var withoutUsings = Write("Service/Folder/WithoutUsings.cs", Utf8WithBom,
            "namespace SWLOR.Game.Server.Service.Folder;\n\npublic static class B { public static long Read() => Environment.TickCount64 + \"caf" + EAcute + "\".Length; }\n");
        var alreadyImported = Write("Service/Folder/AlreadyImported.cs", Utf8WithoutBom,
            "using SWLOR.Game.Server.Service;\nusing SWLOR.Game.Server.Core;\n\nnamespace SWLOR.Game.Server.Service.Folder;\n\npublic static class C { public static object Read() => DateTime.UtcNow; }\n");
        var insideCore = Write("Service/Folder/InsideCore.cs", Utf8WithoutBom,
            "namespace SWLOR.Game.Server.Core\n{\n    public static class D { public static long Read() => Environment.TickCount64; }\n}\n");
        var noReads = Write("Service/Folder/NoReads.cs", Utf8WithoutBom,
            "using System;\r\nnamespace X\n{\n    public static class E { public static object Read() => DateTimeOffset.UtcNow; }\n}");
        var outsideTheManifest = Write("Service/Other.cs", Utf8WithoutBom,
            "namespace X { public static class F { public static object Read() => DateTime.UtcNow; } }\n");
        var unchanged = new Dictionary<string, byte[]>
        {
            [noReads] = File.ReadAllBytes(noReads),
            [outsideTheManifest] = File.ReadAllBytes(outsideTheManifest),
        };

        var first = RunScript();

        first.ExitCode.Should().Be(0, first.Output);
        first.Output.Should().Contain("Changed 4 of 5 files: 6 DateTime.UtcNow and 5 Environment.TickCount64 rewritten, 2 using lines added.");
        File.ReadAllBytes(withUsings).Should().Equal(Utf8WithoutBom.GetBytes(Sample("\r\n", rewritten: true, coreUsing: true)));
        File.ReadAllBytes(withoutUsings).Should().Equal(Utf8WithBom.GetPreamble().Concat(Utf8WithoutBom.GetBytes(
            "using SWLOR.Game.Server.Core;\n\nnamespace SWLOR.Game.Server.Service.Folder;\n\npublic static class B { public static long Read() => Clock.TickCount64 + \"caf" + EAcute + "\".Length; }\n")).ToArray());
        File.ReadAllBytes(alreadyImported).Should().Equal(Utf8WithoutBom.GetBytes(
            "using SWLOR.Game.Server.Service;\nusing SWLOR.Game.Server.Core;\n\nnamespace SWLOR.Game.Server.Service.Folder;\n\npublic static class C { public static object Read() => Clock.UtcNow; }\n"));
        File.ReadAllBytes(insideCore).Should().Equal(Utf8WithoutBom.GetBytes(
            "namespace SWLOR.Game.Server.Core\n{\n    public static class D { public static long Read() => Clock.TickCount64; }\n}\n"));
        foreach (var (file, bytes) in unchanged)
            File.ReadAllBytes(file).Should().Equal(bytes, file);

        var afterFirst = Directory.EnumerateFiles(_temp, "*.cs", SearchOption.AllDirectories)
            .ToDictionary(file => file, file => File.ReadAllBytes(file));

        var second = RunScript();

        second.ExitCode.Should().Be(0, second.Output);
        second.Output.Should().Contain("Changed 0 of 5 files: 0 DateTime.UtcNow and 0 Environment.TickCount64 rewritten, 0 using lines added.");
        foreach (var (file, bytes) in afterFirst)
            File.ReadAllBytes(file).Should().Equal(bytes, file);

        RunScript("-Check").ExitCode.Should().Be(0);
    }

    [Test]
    public void CheckModeChangesNothingAndFailsWhileARewriteIsPending()
    {
        var path = Write("Service/WithUsings.cs", Utf8WithoutBom, Sample("\n", rewritten: false, coreUsing: false));
        var before = File.ReadAllBytes(path);

        var check = RunScript("-Check");

        check.ExitCode.Should().Be(1, check.Output);
        check.Output.Should().Contain("Would change 1 of 1 files");
        File.ReadAllBytes(path).Should().Equal(before);
    }

    // The reads the script must rewrite sit in code; the same text in comments, string literals and escaped interpolation
    // braces must come out byte for byte.
    private const string OriginalBody = """
        namespace SWLOR.Game.Server.Service
        {
            public static class A
            {
                // DateTime.UtcNow in a comment
                public static object Read() => new object[] { DateTime.UtcNow, System.DateTime.UtcNow, Environment.TickCount64, DateTimeOffset.UtcNow, Other.DateTime.UtcNow };
                public static string Text = "DateTime.UtcNow and Environment.TickCount64 stay in strings"; // DateTime.UtcNow
                public static string Verbatim = @"DateTime.UtcNow ""quoted"" Environment.TickCount64";
                public static string Interpolated(int n) => $"DateTime.UtcNow {{Environment.TickCount64}} {Environment.TickCount64} {n}";
                public static string Formatted() => $"{DateTime.UtcNow:yyyy 'DateTime.UtcNow'}";
                public static char Quote = '"'; public static object AfterQuote = DateTime.UtcNow;
                public static object Qualified = new object[] { global::System.DateTime.UtcNow, global::System.Environment.TickCount64 };
            }
        }
        """;

    private const string RewrittenBody = """
        namespace SWLOR.Game.Server.Service
        {
            public static class A
            {
                // DateTime.UtcNow in a comment
                public static object Read() => new object[] { Clock.UtcNow, Clock.UtcNow, Clock.TickCount64, DateTimeOffset.UtcNow, Other.DateTime.UtcNow };
                public static string Text = "DateTime.UtcNow and Environment.TickCount64 stay in strings"; // DateTime.UtcNow
                public static string Verbatim = @"DateTime.UtcNow ""quoted"" Environment.TickCount64";
                public static string Interpolated(int n) => $"DateTime.UtcNow {{Environment.TickCount64}} {Clock.TickCount64} {n}";
                public static string Formatted() => $"{Clock.UtcNow:yyyy 'DateTime.UtcNow'}";
                public static char Quote = '"'; public static object AfterQuote = Clock.UtcNow;
                public static object Qualified = new object[] { Clock.UtcNow, Clock.TickCount64 };
            }
        }
        """;

    /// <summary>A file with a using block (the Core using only when asked) and the sample reads, with the given line endings.</summary>
    private static string Sample(string eol, bool rewritten, bool coreUsing)
    {
        var header = new List<string> { "using System.Collections.Generic;" };
        if (coreUsing)
            header.Add("using SWLOR.Game.Server.Core;");
        header.Add("using SWLOR.Game.Server.Feature.Foo;");
        header.Add("");
        var body = (rewritten ? RewrittenBody : OriginalBody).Replace("\r\n", "\n").Replace("\n", eol);
        return string.Join(eol, header) + eol + body + eol;
    }

    private string Write(string relativePath, Encoding encoding, string content)
    {
        var path = Path.Combine(_temp, "SWLOR.Game.Server", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray());
        return path;
    }

    private (int ExitCode, string Output) RunScript(params string[] arguments)
    {
        var script = Path.Combine(_temp, "tools", "Refactor", "Apply-ClockSeam.ps1");
        foreach (var shell in new[] { "pwsh", "powershell" })
        {
            var start = new ProcessStartInfo(shell)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            if (OperatingSystem.IsWindows())
            {
                start.ArgumentList.Add("-ExecutionPolicy");
                start.ArgumentList.Add("Bypass");
            }

            start.ArgumentList.Add("-File");
            start.ArgumentList.Add(script);
            foreach (var argument in arguments)
                start.ArgumentList.Add(argument);

            Process process;
            try
            {
                process = Process.Start(start)!;
            }
            catch (Win32Exception)
            {
                continue;
            }

            using (process)
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(120_000))
                {
                    process.Kill(true);
                    Assert.Fail("The script did not finish within 120 seconds.");
                }

                process.WaitForExit();
                return (process.ExitCode, output.Result + errors.Result);
            }
        }

        Assert.Ignore("Neither pwsh nor powershell is installed on this machine.");
        return default;
    }
}
