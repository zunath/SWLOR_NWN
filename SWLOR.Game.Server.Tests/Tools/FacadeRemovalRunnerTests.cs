using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.FacadeRemoval;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Runs <c>tools/Refactor/SWLOR.FacadeRemoval</c> over a small repository in a temporary folder: <c>-Apply</c> is
/// idempotent, <c>-Check</c> is clean after it and writes nothing, a tree that still has the facade (master, the parity
/// copy) is left alone, a handler of a removed facade without a rename stops the run, and <c>-Reverse</c> restores the
/// source bytes.
/// </summary>
[TestFixture]
public class FacadeRemovalRunnerTests
{
    private const string Data = """
        { "facades": [ { "facade": "Taxi" } ] }
        """;

    private const string Renames = "# comment\nSWLOR.Game.Server.Service.Taxi.LoadTaxiDestinations SWLOR.Game.Server.Domain.World.TaxiService.LoadTaxiDestinations\n";

    private const string Manifest = "# header\n\nmod_cache_bef  # bucket\n  action      SWLOR.Game.Server.Service.Stat.CacheData\n  action      SWLOR.Game.Server.Service.Taxi.LoadTaxiDestinations\n  action      SWLOR.Game.Server.Service.TaxiRank.Load\n";

    // A server file with a BOM and CRLF line endings, and an engine test project that imports SWLOR.Game.Server.Service globally.
    private static readonly byte[] Dialog = Encoding.UTF8.GetPreamble()
        .Concat(Encoding.UTF8.GetBytes("using SWLOR.Game.Server.Service;\r\n\r\nnamespace SWLOR.Game.Server.Feature\r\n{\r\n    class Dialog { void M() { Taxi.RegisterTaxiDestination(1, 2); } }\r\n}\r\n")).ToArray();

    private const string EngineGlobals = "global using System;\nglobal using SWLOR.Game.Server.Service;\nglobal using SWLOR.Game.Server.Feature;\n";

    private const string EngineTest = "namespace SWLOR.Game.Server.EngineTests\n{\n    class T { void M() { Taxi.GetDestinationsByRegionId(1); } }\n}\n";

    private string _root = null!;

    [SetUp]
    public void CreateRepository()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-facade-removal-" + Guid.NewGuid().ToString("N")[..8]);
        Write("SWLOR.Game.Server.sln", string.Empty);
        Write("SWLOR.Game.Server/SWLOR.Game.Server.csproj", "<Project />");
        Write("SWLOR.Game.Server/Composition/GameServices.cs", "namespace SWLOR.Game.Server.Composition { public static partial class GameServices { } }\n");
        Write("SWLOR.Game.Server/Feature/Dialog.cs", Dialog);
        Write("SWLOR.Game.Server/Feature/Untouched.cs", "class Untouched { }\n");
        Write("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt", Manifest);
        Write("SWLOR.Game.Server/obj/Generated.cs", "class G { void M() { Taxi.Register(1); } }\n");
        Write("SWLOR.Game.Server.EngineTests/SWLOR.Game.Server.EngineTests.csproj", "<Project />");
        Write("SWLOR.Game.Server.EngineTests/GlobalUsings.cs", EngineGlobals);
        Write("SWLOR.Game.Server.EngineTests/Definitions/T.cs", EngineTest);
        Write("tools/Refactor/RemovedFacades.json", Data);
        Write("tools/Refactor/HandlerRenames.txt", Renames);
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void Apply_rewrites_the_callers_and_the_manifest_and_a_second_run_changes_nothing()
    {
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Ok);

        Text("SWLOR.Game.Server/Feature/Dialog.cs").Should().Be(
            "﻿using SWLOR.Game.Server.Service;\r\nusing static SWLOR.Game.Server.Composition.GameServices;\r\n\r\nnamespace SWLOR.Game.Server.Feature\r\n{\r\n    class Dialog { void M() { Taxi.RegisterTaxiDestination(1, 2); } }\r\n}\r\n");
        Text("SWLOR.Game.Server.EngineTests/GlobalUsings.cs").Should().Be(
            "global using System;\nglobal using SWLOR.Game.Server.Service;\nglobal using static SWLOR.Game.Server.Composition.GameServices;\nglobal using SWLOR.Game.Server.Feature;\n");
        Text("SWLOR.Game.Server.EngineTests/Definitions/T.cs").Should().Be(EngineTest, "the project's global usings import GameServices");
        Text("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt").Should().Be(Manifest.Replace(
            "SWLOR.Game.Server.Service.Taxi.LoadTaxiDestinations", "SWLOR.Game.Server.Domain.World.TaxiService.LoadTaxiDestinations"));
        Text("SWLOR.Game.Server/obj/Generated.cs").Should().NotContain("using static", "build output is never touched");

        var after = Snapshot();
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Ok);
        Snapshot().Should().BeEquivalentTo(after, "a second run changes nothing");
        Run(RunMode.Check).Should().Be(FacadeRemovalRunner.Ok, "the check is clean after an apply");
    }

    [Test]
    public void Check_reports_pending_changes_and_writes_nothing()
    {
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Check, log).Should().Be(FacadeRemovalRunner.Differs);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("Would change 3 file(s) for 1 removed facade(s): 2 using line(s) added, 0 call site(s) rewritten, 1 manifest line(s) renamed.");
    }

    [Test]
    public void A_tree_that_still_has_the_facade_is_left_alone()
    {
        Write("SWLOR.Game.Server/Service/Taxi.cs", "namespace SWLOR.Game.Server.Service { public static class Taxi { } }\n");
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(FacadeRemovalRunner.Ok);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("Skipped Taxi: SWLOR.Game.Server/Service/Taxi.cs still exists in this tree");
        Run(RunMode.Check).Should().Be(FacadeRemovalRunner.Ok);
    }

    [Test]
    public void A_tree_without_the_accessor_is_not_the_branch_and_is_left_alone()
    {
        // Master has no GameServices, and a facade born on the branch has no file there either.
        File.Delete(Path.Combine(_root, FacadeRemovalRunner.AccessorPath));
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(FacadeRemovalRunner.Ok);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("Skipped every facade").And.Contain("Changed 0 file(s) for 0 removed facade(s)");
    }

    [Test]
    public void A_handler_of_a_removed_facade_without_a_rename_stops_the_run_and_nothing_is_written()
    {
        Write("tools/Refactor/HandlerRenames.txt", "# none yet\n");
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(FacadeRemovalRunner.Failed);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("does not rename").And.Contain("line 5: SWLOR.Game.Server.Service.Taxi.LoadTaxiDestinations");
    }

    [TestCase("{ \"facades\": [ { \"facade\": \"Taxi\" }, { \"facade\": \"Bank\" } ] }", "Bank is out of order")]
    [TestCase("{ \"facades\": [ { \"facade\": \"Taxi\" }, { \"facade\": \"Taxi\" } ] }", "Taxi is listed twice")]
    [TestCase("{ \"facades\": [ { \"name\": \"Taxi\" } ] }", "needs a \"facade\"")]
    [TestCase("{ \"facades\": [ { \"facade\": \"Taxi\", \"explicit\": 1 } ] }", "\"explicit\" must be true or false")]
    [TestCase("{ \"facades\": [ { \"facade\": \"Taxi\", \"constants\": {} } ] }", "\"constants\" must be an array")]
    [TestCase("{ \"facades\": [ { \"facade\": \"Taxi\", \"constants\": [ { \"name\": \"X\", \"owner\": \"Rules\" } ] } ] }", "full type name of its \"owner\"")]
    [TestCase("{ \"facades\": [ { \"facade\": \"A.Taxi\" }, { \"facade\": \"Taxi\" } ] }", "same simple name")]
    [TestCase("{ \"facades\": [ { \"facade\": \"A\", \"constants\": [ { \"name\": \"X\", \"owner\": \"N.R\" } ] }, { \"facade\": \"B\", \"constants\": [ { \"name\": \"X\", \"owner\": \"N.R\" }, { \"name\": \"X\", \"owner\": \"N.S\" } ] } ] }", "N.R.X is the owner of a constant of more than one facade")]
    [TestCase("{ \"facades\": [ { \"facade\": \"B\", \"constants\": [ { \"name\": \"X\", \"owner\": \"N.R\" }, { \"name\": \"X\", \"owner\": \"N.S\" } ] } ] }", "the constant X is listed twice")]
    [TestCase("{ \"list\": [] }", "must be an object with a \"facades\" array")]
    [TestCase("{ nope", "is not valid JSON")]
    public void A_malformed_data_file_stops_the_run(string json, string problem)
    {
        Write("tools/Refactor/RemovedFacades.json", json);
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(FacadeRemovalRunner.Failed);

        log.ToString().Should().Contain(problem);
        Text("SWLOR.Game.Server/Feature/Dialog.cs").Should().NotContain("using static");
    }

    [Test]
    public void A_malformed_renames_file_or_a_missing_data_file_stops_the_run()
    {
        Write("tools/Refactor/HandlerRenames.txt", "only-one-name\n");
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Failed);

        Write("tools/Refactor/HandlerRenames.txt", "a b\na c\n");
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Failed);

        File.Delete(Path.Combine(_root, "tools/Refactor/RemovedFacades.json"));
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Failed);
    }

    [Test]
    public void Reverse_restores_the_source_bytes_of_a_folder()
    {
        var originals = Snapshot();
        Run(RunMode.Apply).Should().Be(FacadeRemovalRunner.Ok);
        var log = new StringWriter();

        FacadeRemovalRunner.Run(new RunOptions(RunMode.Reverse, Path.Combine(_root, "SWLOR.Game.Server"), DataPath), log).Should().Be(FacadeRemovalRunner.Ok);
        FacadeRemovalRunner.Run(new RunOptions(RunMode.Reverse, Path.Combine(_root, "SWLOR.Game.Server.EngineTests"), DataPath), log).Should().Be(FacadeRemovalRunner.Ok);

        Bytes("SWLOR.Game.Server/Feature/Dialog.cs").Should().Equal(originals["SWLOR.Game.Server/Feature/Dialog.cs"]);
        Bytes("SWLOR.Game.Server.EngineTests/GlobalUsings.cs").Should().Equal(originals["SWLOR.Game.Server.EngineTests/GlobalUsings.cs"]);
        log.ToString().Should().Contain("Reversed 1 file(s): 1 using line(s) removed");
        FacadeRemovalRunner.Run(new RunOptions(RunMode.Reverse, Path.Combine(_root, "missing"), DataPath), log).Should().Be(FacadeRemovalRunner.Failed);
    }

    [Test]
    public void The_branch_is_in_the_shape_the_tool_keeps_it_in()
    {
        var root = RepositoryLocator.Find(TestContext.CurrentContext.TestDirectory)!;
        var log = new StringWriter();

        var result = FacadeRemovalRunner.Run(new RunOptions(RunMode.Check, root, Path.Combine(root, "tools", "Refactor", "RemovedFacades.json")), log);

        result.Should().Be(FacadeRemovalRunner.Ok, $"Apply-FacadeRemoval.ps1 -Check must be clean on the branch:{Environment.NewLine}{log}");
    }

    private string DataPath => Path.Combine(_root, "tools", "Refactor", "RemovedFacades.json");

    private int Run(RunMode mode, StringWriter? log = null) =>
        FacadeRemovalRunner.Run(new RunOptions(mode, _root, DataPath), log ?? new StringWriter());

    private void Write(string relative, string text) => Write(relative, Encoding.UTF8.GetBytes(text));

    private void Write(string relative, byte[] bytes)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private byte[] Bytes(string relative) => File.ReadAllBytes(Path.Combine(_root, relative));

    private string Text(string relative) => Encoding.UTF8.GetString(Bytes(relative));

    private Dictionary<string, byte[]> Snapshot() =>
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(_root, path).Replace('\\', '/'), File.ReadAllBytes);
}
