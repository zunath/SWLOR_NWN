using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Runs the handler-name helpers of <c>tools/Refactor/RefactorCommon.ps1</c> and <c>MasterCopy.ps1</c> on a small throwaway
/// repository: master's handler names meet the branch's manifest under the P2-Z2 namespace moves, both before the move (the
/// sync reads master as it is) and after it (the parity copy, where <c>Move-SupportNamespaces.ps1</c> ran).
/// </summary>
/// <remarks>
/// The fixtures use a made-up service (<c>Widget</c>): <c>Move-SupportNamespaces.ps1</c> rewrites every text file of the
/// repository, so a real old name here would be rewritten when master brings back its support folder.
/// </remarks>
[TestFixture]
public class HandlerRenameNamespaceMoveTests
{
    private const string DomainMap = """
        { "domains": { "Gadgets": { "services": [ "Widget" ], "featureFolders": [] }, "Base": { "services": [ "Ledger" ], "featureFolders": [] } } }
        """;

    private const string Moves = """
        {
          "namespaceRenames": [ { "from": "Domain.Base", "to": "Domain.Foundation" }, { "from": "Feature.WidgetDefinition.Gear", "to": "Feature.WidgetDefinition.GearParts" } ],
          "retiredServiceFolders": [ { "service": "Tool", "domain": "Gadgets" } ]
        }
        """;

    private const string Server = "SWLOR.Game.Server.";

    // The branch's manifest: one handler renamed by the branch (its master name is in the support namespace), one only
    // moved, one of a retired folder, one under a renamed namespace, one that never moved.
    private const string Manifest = "mod_load\n"
        + "  action      " + Server + "Domain.Gadgets.WidgetEncounterService.OnEnter\n"
        + "  action      " + Server + "Domain.Gadgets.WidgetStore.Load\n"
        + "  action      " + Server + "Domain.Gadgets.ToolPanel.Open\n"
        + "  action      " + Server + "Domain.Foundation.LedgerSink.Flush\n"
        + "  action      " + Server + "Feature.WidgetDefinition.GearParts.Cog.Spin\n"
        + "  action      " + Server + "Feature.Other.Keep\n";

    private const string Renames = "# master branch\n"
        + Server + "Service.WidgetService.WidgetEncounter.OnEnter " + Server + "Domain.Gadgets.WidgetEncounterService.OnEnter\n"
        + Server + "Service.WidgetService.WidgetStore.Load " + Server + "Domain.Gadgets.WidgetStore.Load\n";

    private string _temp = null!;

    [SetUp]
    public void CreateRepository()
    {
        _temp = Path.Combine(Path.GetTempPath(), "handler-renames-" + Guid.NewGuid().ToString("N"));
        var refactor = Path.Combine(_temp, "tools", "Refactor");
        Directory.CreateDirectory(refactor);
        var root = ClockSeamSources.FindRepositoryRoot();
        foreach (var script in new[] { "RefactorCommon.ps1", "MasterCopy.ps1" })
            File.Copy(Path.Combine(root, "tools", "Refactor", script), Path.Combine(refactor, script));
        Write("tools/Refactor/NamespaceMoves.json", Moves);
        Write("tools/Refactor/HandlerRenames.txt", Renames);
        Write("SWLOR.Architecture.Tests/DomainMap.json", DomainMap);
        Write("manifest.txt", Manifest);
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    [Test]
    public void MastersNamesBeforeTheMoveTakeTheBranchNames()
    {
        // What the sync reads: master's own names, in the support namespaces.
        var names = Convert("mod_load\n"
            + "  action      " + Server + "Service.WidgetService.WidgetEncounter.OnEnter\n"
            + "  action      " + Server + "Service.WidgetService.WidgetStore.Load\n"
            + "  action      " + Server + "Service.ToolService.ToolPanel.Open\n"
            + "  action      " + Server + "Service.LedgerService.LedgerSink.Flush\n"
            + "  action      " + Server + "Feature.WidgetDefinition.Gear.Cog.Spin\n"
            + "  action      " + Server + "Feature.Other.Keep\n"
            + "  action      " + Server + "Service.WidgetService.WidgetNew.OnNew\n"
            + "  action      " + Server + "Service.Widget.OnFacade\n");

        names.Should().Equal(
            Server + "Domain.Gadgets.WidgetEncounterService.OnEnter",
            Server + "Domain.Gadgets.WidgetStore.Load",
            Server + "Domain.Gadgets.ToolPanel.Open",
            Server + "Domain.Foundation.LedgerSink.Flush",
            Server + "Feature.WidgetDefinition.GearParts.Cog.Spin",
            Server + "Feature.Other.Keep",
            Server + "Domain.Gadgets.WidgetNew.OnNew",
            Server + "Service.Widget.OnFacade");
    }

    [Test]
    public void MastersNamesAfterTheMoveTakeTheBranchNames()
    {
        // What the parity copy reads once Move-SupportNamespaces.ps1 ran on it: the renamed handler still finds its line.
        var names = Convert("mod_load\n"
            + "  action      " + Server + "Domain.Gadgets.WidgetEncounter.OnEnter\n"
            + "  action      " + Server + "Domain.Gadgets.WidgetStore.Load\n"
            + "  action      " + Server + "Domain.Foundation.LedgerSink.Flush\n"
            + "  action      " + Server + "Feature.WidgetDefinition.GearParts.Cog.Spin\n");

        names.Should().Equal(
            Server + "Domain.Gadgets.WidgetEncounterService.OnEnter",
            Server + "Domain.Gadgets.WidgetStore.Load",
            Server + "Domain.Foundation.LedgerSink.Flush",
            Server + "Feature.WidgetDefinition.GearParts.Cog.Spin");
    }

    [Test]
    public void ATypeMovedOutOfServiceTakesItsNewNameWithOrWithoutARenameLine()
    {
        // P2-Z3 (tools/Refactor/TypeMoves.json) moved single types out of Service; their handlers carry the new type name.
        Write("tools/Refactor/TypeMoves.json", "{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ "
            + "{ \"type\": \"Gauge\", \"to\": \"SWLOR.Game.Server.Domain.Gadgets\" }, { \"type\": \"Meter\", \"to\": \"SWLOR.Game.Server.Domain.Foundation\" } ] }");
        Write("manifest.txt", Manifest + "  action      " + Server + "Domain.Gadgets.Gauge.Load\n  action      " + Server + "Domain.Foundation.Meter.Tick\n");
        Write("tools/Refactor/HandlerRenames.txt", Renames + Server + "Service.Meter.Tick " + Server + "Domain.Foundation.Meter.Tick\n");

        // What the sync reads (master's names) and what the parity copy reads once Move-ServiceTypes.ps1 ran on it.
        Convert("mod_load\n"
                + "  action      " + Server + "Service.Gauge.Load\n"
                + "  action      " + Server + "Service.Meter.Tick\n"
                + "  action      " + Server + "Service.Widget.OnFacade\n")
            .Should().Equal(Server + "Domain.Gadgets.Gauge.Load", Server + "Domain.Foundation.Meter.Tick", Server + "Service.Widget.OnFacade");
        Convert("mod_load\n"
                + "  action      " + Server + "Domain.Gadgets.Gauge.Load\n"
                + "  action      " + Server + "Domain.Foundation.Meter.Tick\n")
            .Should().Equal(Server + "Domain.Gadgets.Gauge.Load", Server + "Domain.Foundation.Meter.Tick");
    }

    [Test]
    public void WithoutTheMoveDataTheRenamesAloneApply()
    {
        File.Delete(Path.Combine(_temp, "tools", "Refactor", "NamespaceMoves.json"));

        var names = Convert("mod_load\n"
            + "  action      " + Server + "Service.WidgetService.WidgetEncounter.OnEnter\n"
            + "  action      " + Server + "Service.WidgetService.WidgetNew.OnNew\n");

        names.Should().Equal(Server + "Domain.Gadgets.WidgetEncounterService.OnEnter", Server + "Service.WidgetService.WidgetNew.OnNew");
    }

    [Test]
    public void AHandlerTheBranchSplitBecomesAllItsPartsInOrderWhereMastersOneHandlerWas()
    {
        // P4-8: master's one handler is two on the branch, a part a hot reload reruns and a boot-only part.
        Write("manifest.txt", Manifest
            + "mod_preload\n"
            + "  action      " + Server + "Domain.Gadgets.GaugeService.Connect\n"
            + "  action      " + Server + "Domain.Gadgets.GaugeService.SyncIndexes\n"
            + "  action      " + Server + "Feature.Boot.StartEvents\n"
            + "  action      " + Server + "Feature.Boot.OnPreload\n");
        Write("tools/Refactor/HandlerRenames.txt", Renames
            + Server + "Service.Gauge.Load " + Server + "Domain.Gadgets.GaugeService.Connect " + Server + "Domain.Gadgets.GaugeService.SyncIndexes\n"
            + Server + "Feature.Boot.OnPreload " + Server + "Feature.Boot.StartEvents " + Server + "Feature.Boot.OnPreload\n");

        var names = Convert("mod_load\n"
            + "  action      " + Server + "Feature.Other.Keep\n"
            + "  action      " + Server + "Service.Gauge.Load\n"
            + "  action      " + Server + "Feature.Boot.OnPreload\n"
            + "  action      " + Server + "Service.WidgetService.WidgetStore.Load\n");

        names.Should().Equal(
            Server + "Feature.Other.Keep",
            Server + "Domain.Gadgets.GaugeService.Connect",
            Server + "Domain.Gadgets.GaugeService.SyncIndexes",
            Server + "Feature.Boot.StartEvents",
            Server + "Feature.Boot.OnPreload",
            Server + "Domain.Gadgets.WidgetStore.Load");
    }

    [TestCase("Domain.Gadgets.WidgetStore.Load Feature.Other.Missing", "is not a line of the handler manifest")]
    [TestCase("Domain.Gadgets.WidgetStore.Load Domain.Gadgets.WidgetStore.Load", "is listed twice on the line")]
    public void ASplitLineWhosePartsAreNotDistinctLinesOfTheManifestIsRefused(string parts, string problem)
    {
        var branchNames = string.Join(" ", parts.Split(' ').Select(name => Server + name));
        Write("tools/Refactor/HandlerRenames.txt", Server + "Service.Gauge.Load " + branchNames + "\n");

        var command = "$ErrorActionPreference = 'Stop'; "
            + ". ./tools/Refactor/RefactorCommon.ps1; . ./tools/Refactor/MasterCopy.ps1; "
            + "try { Read-HandlerRenames -ManifestText ([IO.File]::ReadAllText((Join-Path $PWD 'manifest.txt'))) | Out-Null } "
            + "catch { 'REFUSED ' + $_.Exception.Message; exit 1 }";
        var (exitCode, output) = RunPowerShell(command);

        exitCode.Should().NotBe(0, output);
        output.Should().Contain("REFUSED ").And.Contain(problem);
    }

    /// <summary>Reads the renames against the manifest and converts a master handler order text; returns the action names.</summary>
    private IReadOnlyList<string> Convert(string reference)
    {
        Write("reference.txt", reference);
        var command = "$ErrorActionPreference = 'Stop'; "
            + ". ./tools/Refactor/RefactorCommon.ps1; . ./tools/Refactor/MasterCopy.ps1; "
            + "$renames = Read-HandlerRenames -ManifestText ([IO.File]::ReadAllText((Join-Path $PWD 'manifest.txt'))); "
            + "$parsed = Convert-HandlerOrderNames (ConvertFrom-HandlerOrderText ([IO.File]::ReadAllText((Join-Path $PWD 'reference.txt')))) $renames; "
            + "foreach ($name in $parsed['mod_load'].action) { 'NAME ' + $name }";
        var (exitCode, output) = RunPowerShell(command);
        exitCode.Should().Be(0, output);
        return output.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("NAME ", StringComparison.Ordinal))
            .Select(line => line["NAME ".Length..])
            .ToList();
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_temp, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private (int ExitCode, string Output) RunPowerShell(string command)
    {
        foreach (var shell in new[] { "pwsh", "powershell" })
        {
            var start = new ProcessStartInfo(shell)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _temp,
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            if (OperatingSystem.IsWindows())
            {
                start.ArgumentList.Add("-ExecutionPolicy");
                start.ArgumentList.Add("Bypass");
            }

            start.ArgumentList.Add("-Command");
            start.ArgumentList.Add(command);

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
                    Assert.Fail("PowerShell did not finish within 120 seconds.");
                }

                process.WaitForExit();
                return (process.ExitCode, output.Result + errors.Result);
            }
        }

        Assert.Ignore("Neither pwsh nor powershell is installed on this machine.");
        return default;
    }
}
