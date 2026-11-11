using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Runs <c>tools/Refactor/Move-SupportNamespaces.ps1</c> on a small throwaway repository: the domain comes from the domain
/// map, using, using static, alias and fully qualified names follow the move, the handler manifest and the rename list are
/// rewritten, a clash is refused, and a second run changes nothing.
/// </summary>
[TestFixture]
public class MoveSupportNamespacesScriptTests
{
    private const string DomainMap = """
        {
          "domains": {
            "Core": { "services": [ "Log" ], "featureFolders": [] },
            "Crafting": { "services": [ "Craft" ], "featureFolders": [] }
          }
        }
        """;

    private const string Moves = """
        {
          "namespaceRenames": [ { "from": "Domain.Core", "to": "Domain.Foundation" } ],
          "textExcludes": [],
          "facadeNameFiles": [],
          "textEdits": []
        }
        """;

    private const string Manifest = "  action      SWLOR.Game.Server.Service.CraftService.Foo.OnEnter\n"
        + "  action      SWLOR.Game.Server.Service.Craft.OnLeave\n";

    private const string HandlerRenames = "# sample\n"
        + "SWLOR.Game.Server.Service.CraftService.Foo.Master SWLOR.Game.Server.Service.CraftService.Foo.Other\n";

    private string _temp = null!;

    [SetUp]
    public void CreateRepository()
    {
        _temp = Path.Combine(Path.GetTempPath(), "move-namespaces-" + Guid.NewGuid().ToString("N"));
        var refactor = Path.Combine(_temp, "tools", "Refactor");
        Directory.CreateDirectory(refactor);
        var root = ClockSeamSources.FindRepositoryRoot();
        File.Copy(Path.Combine(root, "tools", "Refactor", "Move-SupportNamespaces.ps1"), Path.Combine(refactor, "Move-SupportNamespaces.ps1"));
        File.WriteAllText(Path.Combine(refactor, "NamespaceMoves.json"), Moves);
        File.WriteAllText(Path.Combine(refactor, "HandlerRenames.txt"), HandlerRenames);
        Write("SWLOR.Architecture.Tests/DomainMap.json", DomainMap);
        Write("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt", Manifest);
        Write("SWLOR.Game.Server/Service/Craft.cs",
            "namespace SWLOR.Game.Server.Service;\n\npublic static class Craft { public static void Do() { } }\n");
        Write("SWLOR.Game.Server/Service/CraftService/Foo.cs",
            "using System;\nusing SWLOR.Game.Server.Service.LogService;\n\nnamespace SWLOR.Game.Server.Service.CraftService;\n\npublic static class Foo\n{\n    public static void M() => Craft.Do();\n}\n");
        Write("SWLOR.Game.Server/Service/CraftService/Minigame/Bar.cs",
            "namespace SWLOR.Game.Server.Service.CraftService.Minigame;\n\npublic static class Bar { }\n");
        Write("SWLOR.Game.Server/Service/LogService/Baz.cs",
            "namespace SWLOR.Game.Server.Service.LogService;\n\npublic static class Baz { public const int Value = 1; }\n");
        Write("SWLOR.Game.Server/Domain/Core/X.cs",
            "namespace SWLOR.Game.Server.Domain.Core;\n\npublic static class X { }\n");
        Write("SWLOR.Game.Server.Tests/User.cs",
            "using SWLOR.Game.Server.Service.CraftService;\n"
            + "using SWLOR.Game.Server.Service.CraftService.Minigame;\n"
            + "using static SWLOR.Game.Server.Service.CraftService.Foo;\n"
            + "using Alias = SWLOR.Game.Server.Service.CraftService.Minigame.Bar;\n"
            + "using SWLOR.Game.Server.Domain.Core;\n\n"
            + "namespace SWLOR.Game.Server.Tests;\n\n"
            + "public class User\n{\n"
            + "    public int Read() => SWLOR.Game.Server.Service.LogService.Baz.Value;\n"
            + "    public string Path1 = Path.Combine(\"SWLOR.Game.Server\", \"Service\", \"CraftService\", \"Foo.cs\");\n"
            + "    public string Path2 = \"SWLOR.Game.Server/Service/LogService/Baz.cs\";\n"
            + "}\n");
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    [Test]
    public void MovesTheNamespacesByTheDomainMapAndRewritesEveryKindOfReference()
    {
        var check = RunScript("-Check");
        check.ExitCode.Should().Be(1, check.Output);
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "CraftService", "Foo.cs")).Should().BeTrue("-Check changes nothing");

        var run = RunScript("-Apply");

        run.ExitCode.Should().Be(0, run.Output);
        Read("SWLOR.Game.Server/Domain/Crafting/Foo.cs").Should()
            .Contain("namespace SWLOR.Game.Server.Domain.Crafting;")
            .And.Contain("using SWLOR.Game.Server.Domain.Foundation;")
            .And.Contain("using SWLOR.Game.Server.Service;", "the facade Craft is no longer in the parent namespace");
        Read("SWLOR.Game.Server/Domain/Crafting/Minigame/Bar.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Crafting.Minigame;");
        Read("SWLOR.Game.Server/Domain/Foundation/Baz.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Foundation;");
        Read("SWLOR.Game.Server/Domain/Foundation/X.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Foundation;");
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "CraftService", "Foo.cs")).Should().BeFalse();
        Directory.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "CraftService")).Should().BeFalse();
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "Craft.cs")).Should().BeTrue("the facade stays");

        var user = Read("SWLOR.Game.Server.Tests/User.cs");
        user.Should().Contain("using SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Crafting.Minigame;\n");
        user.Should().Contain("using static SWLOR.Game.Server.Domain.Crafting.Foo;");
        user.Should().Contain("using Alias = SWLOR.Game.Server.Domain.Crafting.Minigame.Bar;");
        user.Should().Contain("using SWLOR.Game.Server.Domain.Foundation;");
        user.Should().Contain("=> SWLOR.Game.Server.Domain.Foundation.Baz.Value;");
        user.Should().Contain("\"SWLOR.Game.Server\", \"Domain\", \"Crafting\", \"Foo.cs\"");
        user.Should().Contain("\"SWLOR.Game.Server/Domain/Foundation/Baz.cs\"");
        user.Should().NotContain("Service.CraftService").And.NotContain("Service.LogService");
    }

    [Test]
    public void RewritesTheHandlerManifestAndTheRenameList()
    {
        RunScript("-Apply").ExitCode.Should().Be(0);

        Read("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt").Should().Be(
            "  action      SWLOR.Game.Server.Domain.Crafting.Foo.OnEnter\n"
            + "  action      SWLOR.Game.Server.Service.Craft.OnLeave\n");
        Read("tools/Refactor/HandlerRenames.txt").Should().Be("# sample\n"
            + "SWLOR.Game.Server.Service.CraftService.Foo.Master SWLOR.Game.Server.Service.CraftService.Foo.Other\n"
            + "SWLOR.Game.Server.Service.CraftService.Foo.OnEnter SWLOR.Game.Server.Domain.Crafting.Foo.OnEnter\n");
    }

    [Test]
    public void ASecondApplyChangesNothingAndCheckPasses()
    {
        RunScript("-Apply").ExitCode.Should().Be(0);
        var after = Snapshot();

        var second = RunScript("-Apply");

        second.ExitCode.Should().Be(0, second.Output);
        second.Output.Should().Contain("moved 0 files, rewrote 0 files");
        Snapshot().Should().BeEquivalentTo(after);
        RunScript("-Check").ExitCode.Should().Be(0);
    }

    [Test]
    public void TwoFilesOnTheSamePathAreRefusedAndNothingChanges()
    {
        Write("SWLOR.Game.Server/Service/CraftService/Dup.cs", "namespace SWLOR.Game.Server.Service.CraftService;\n\npublic class Dup { }\n");
        Write("SWLOR.Game.Server/Domain/Crafting/Dup.cs", "namespace SWLOR.Game.Server.Domain.Crafting;\n\npublic class Dup { }\n");
        var before = Snapshot();

        var run = RunScript("-Apply");

        run.ExitCode.Should().Be(2, run.Output);
        run.Output.Should().Contain("Would land on the same path").And.Contain("Dup.cs");
        Snapshot().Should().BeEquivalentTo(before);
    }

    [Test]
    public void AServiceMissingFromTheDomainMapIsReportedNotGuessed()
    {
        Write("SWLOR.Game.Server/Service/GhostService/Ghost.cs", "namespace SWLOR.Game.Server.Service.GhostService;\n\npublic class Ghost { }\n");

        var run = RunScript("-Check");

        run.ExitCode.Should().Be(2, run.Output);
        run.Output.Should().Contain("Service 'Ghost'").And.Contain("missing from DomainMap.json");
    }

    [Test]
    public void ReportListsTheRemainingServiceNamespaces()
    {
        var run = RunScript("-Report");

        run.ExitCode.Should().Be(1, run.Output);
        run.Output.Should().Contain("Remaining Service.*Service namespaces: 3")
            .And.Contain("SWLOR.Game.Server.Service.CraftService.Minigame");

        RunScript("-Apply").ExitCode.Should().Be(0);
        var done = RunScript("-Report");
        done.Output.Should().Contain("Remaining Service.*Service namespaces: 0");
    }

    [Test]
    public void WithoutAModeItApplies_AndItOwnsNoFile()
    {
        // tools/Refactor/MechanicalScripts.txt contract: the sync runs a script without a mode to apply it, -Check to
        // check it and -ListOwnedFiles for the files whose conflicts it settles by taking master's version.
        var list = RunScript("-ListOwnedFiles");
        list.ExitCode.Should().Be(0, list.Output);
        list.Output.Trim().Should().BeEmpty("the script owns no whole file");
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "CraftService", "Foo.cs")).Should().BeTrue("-ListOwnedFiles changes nothing");

        var run = RunScript();

        run.ExitCode.Should().Be(0, run.Output);
        Read("SWLOR.Game.Server/Domain/Crafting/Foo.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Crafting;");
        RunScript("-Check").ExitCode.Should().Be(0);
        RunScript("-Check", "-Apply").ExitCode.Should().Be(2, "one mode at most");
    }

    [Test]
    public void ARetiredServiceFolderMovesToTheDomainTheDataGivesIt()
    {
        File.WriteAllText(Path.Combine(_temp, "tools", "Refactor", "NamespaceMoves.json"), Moves.Replace(
            "\"textEdits\": []", "\"textEdits\": [], \"retiredServiceFolders\": [ { \"service\": \"Tool\", \"domain\": \"Crafting\" } ]"));
        Write("SWLOR.Game.Server/Service/ToolService/Old.cs", "namespace SWLOR.Game.Server.Service.ToolService;\n\npublic class Old { }\n");

        var run = RunScript("-Apply");

        run.ExitCode.Should().Be(0, run.Output);
        Read("SWLOR.Game.Server/Domain/Crafting/Old.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Crafting;");
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "ToolService", "Old.cs")).Should().BeFalse();
    }

    [Test]
    public void AMovedFileKeepsReachingTheTypesP2Z3MovedOutOfServiceThroughService()
    {
        // TypeMoves.json beside the script lists types the branch moved out of Service. Master's code still reaches them
        // through the enclosing namespace Service, so a moved master file imports Service and Move-ServiceTypes.ps1 (next in
        // MechanicalScripts.txt) moves the reference to the type's domain.
        File.WriteAllText(Path.Combine(_temp, "tools", "Refactor", "TypeMoves.json"),
            "{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"Gauge\", \"to\": \"SWLOR.Game.Server.Domain.Foundation\" } ] }");
        Write("SWLOR.Game.Server/Service/LogService/Reader.cs",
            "namespace SWLOR.Game.Server.Service.LogService;\n\npublic static class Reader\n{\n    public static int Read() => Gauge.Value;\n}\n");
        Write("SWLOR.Game.Server/Service/LogService/Quiet.cs",
            "namespace SWLOR.Game.Server.Service.LogService;\n\npublic static class Quiet\n{\n    public static int Read() => 1;\n}\n");

        var run = RunScript("-Apply");

        run.ExitCode.Should().Be(0, run.Output);
        Read("SWLOR.Game.Server/Domain/Foundation/Reader.cs").Should().StartWith("using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Domain.Foundation;");
        Read("SWLOR.Game.Server/Domain/Foundation/Quiet.cs").Should().NotContain("using SWLOR.Game.Server.Service;", "it names nothing of Service");
    }

    [Test]
    public void WithNothingLeftInServiceAMovedFileGetsNoUsingOfIt()
    {
        File.Delete(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "Craft.cs"));

        var run = RunScript("-Apply");

        run.ExitCode.Should().Be(0, run.Output);
        Read("SWLOR.Game.Server/Domain/Crafting/Foo.cs").Should().NotContain("using SWLOR.Game.Server.Service;", "no type is left in Service to reach");
    }

    [Test]
    public void ATreeWithoutADomainMapMovesByTheMapOfTheScriptsRepository()
    {
        // Master's copy in the parity check has no SWLOR.Architecture.Tests: the branch's map, beside the script, decides.
        var tree = Path.Combine(_temp, "master-copy");
        Write("master-copy/SWLOR.Game.Server/Service/CraftService/Foo.cs",
            "namespace SWLOR.Game.Server.Service.CraftService;\n\npublic static class Foo { }\n");

        var run = RunScript("-Apply", "-RepositoryRoot", tree);

        run.ExitCode.Should().Be(0, run.Output);
        Read("master-copy/SWLOR.Game.Server/Domain/Crafting/Foo.cs").Should().Contain("namespace SWLOR.Game.Server.Domain.Crafting;");
        File.Exists(Path.Combine(_temp, "SWLOR.Game.Server", "Service", "CraftService", "Foo.cs")).Should().BeTrue("only the tree it was given moves");
    }

    private string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(_temp, relativePath.Replace('/', Path.DirectorySeparatorChar)), Encoding.Latin1);

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_temp, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private Dictionary<string, string> Snapshot() =>
        Directory.EnumerateFiles(_temp, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(_temp, file), file => File.ReadAllText(file, Encoding.Latin1));

    private (int ExitCode, string Output) RunScript(params string[] arguments)
    {
        var script = Path.Combine(_temp, "tools", "Refactor", "Move-SupportNamespaces.ps1");
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
