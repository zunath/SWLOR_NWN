using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Tests.Core.ScriptHandlerTestSupport;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The parity tool (<c>tools/Refactor/Test-MaintainabilityParity.ps1</c>) against the handlers Phase 4 changed (P4-16). It
/// compares master's reflection order with the branch's handler files by handler name, so (a) a handler master has once and
/// the branch splits into parts (P4-8) must map onto its parts through <c>HandlerRenames.txt</c>, and (b) the reload
/// classification attributes (<c>[BootPhase]</c>, <c>[PlayerSession]</c>, <c>[ReloadState]</c>) must never reach those files.
/// </summary>
[TestFixture]
public class ParityHandlerMappingTests
{
    private const string ManifestPath = "SWLOR.Game.Server/Core/ScriptHandlerOrder.txt";
    private const string EngineSnapshotPath = "SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.EngineTests.txt";

    private static readonly Regex HandlerLine = new(@"^\s+(action|conditional)\s+[A-Za-z0-9_.`+]+(\s+#.*)?$", RegexOptions.Compiled);

    private string _temp = null!;

    [SetUp]
    public void CreateRepository()
    {
        _temp = Path.Combine(Path.GetTempPath(), "parity-mapping-" + Guid.NewGuid().ToString("N"));
        var refactor = Path.Combine(_temp, "tools", "Refactor");
        Directory.CreateDirectory(refactor);
        var root = ClockSeamSources.FindRepositoryRoot();
        foreach (var file in new[] { "RefactorCommon.ps1", "MasterCopy.ps1", "HandlerRenames.txt" })
            File.Copy(Path.Combine(root, "tools", "Refactor", file), Path.Combine(refactor, file));
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    [TestCase(ManifestPath)]
    [TestCase(EngineSnapshotPath)]
    public void The_handler_files_the_parity_run_compares_hold_handler_names_and_no_classification(string path)
    {
        var lines = File.ReadAllLines(Path.Combine(ClockSeamSources.FindRepositoryRoot(), path.Replace('/', Path.DirectorySeparatorChar)))
            .Where(line => line.Trim().Length > 0 && !line.TrimStart().StartsWith('#') && !line.StartsWith("==", StringComparison.Ordinal))
            .ToList();

        foreach (var line in lines.Where(line => char.IsWhiteSpace(line[0])))
        {
            HandlerLine.IsMatch(line).Should().BeTrue($"'{line}' is a kind and a Namespace.Type.Method name, nothing the attributes add");
            line.Should().NotContainAny("BootPhase", "PlayerSession", "ReloadState");
        }
    }

    [Test]
    public void A_classified_handler_is_named_and_listed_like_any_other_handler()
    {
        var manifest = ScriptHandlerManifest.Parse(ShippedManifestText(), "ScriptHandlerOrder.txt");
        var listed = manifest.ScriptNames
            .SelectMany(script => manifest.GetHandlers(script, false).Concat(manifest.GetHandlers(script, true)))
            .ToHashSet(StringComparer.Ordinal);

        var classified = MethodsOf(GameAssembly)
            .Where(IsHandler)
            .Where(method => method.GetCustomAttribute<BootPhaseAttribute>() != null || method.GetCustomAttribute<PlayerSessionAttribute>() != null)
            .ToList();

        classified.Should().NotBeEmpty();
        foreach (var method in classified)
        {
            ScriptHandlerManifest.HandlerName(method).Should().Be(method.DeclaringType!.FullName + "." + method.Name, "the name ignores attributes");
            listed.Should().Contain(ScriptHandlerManifest.HandlerName(method), $"{method.DeclaringType.Name}.{method.Name} keeps its manifest line");
        }
    }

    [Test]
    public void Every_split_line_of_the_real_renames_maps_masters_one_handler_onto_its_parts_in_order()
    {
        var root = ClockSeamSources.FindRepositoryRoot();
        var splits = File.ReadAllLines(Path.Combine(root, "tools", "Refactor", "HandlerRenames.txt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(names => names.Length > 2)
            .ToList();
        splits.Should().NotBeEmpty();

        // Master's order text: the other handlers around the split ones, each split handler once under master's name.
        var reference = new StringBuilder("mod_load\n");
        foreach (var names in splits)
            reference.Append("  action      ").Append(names[0]).Append('\n');
        File.WriteAllText(Path.Combine(_temp, "reference.txt"), reference.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(_temp, "manifest.txt"), File.ReadAllText(Path.Combine(root, ManifestPath)), new UTF8Encoding(false));

        var command = "$ErrorActionPreference = 'Stop'; "
            + ". ./tools/Refactor/RefactorCommon.ps1; . ./tools/Refactor/MasterCopy.ps1; "
            + "$renames = Read-HandlerRenames -ManifestText ([IO.File]::ReadAllText((Join-Path $PWD 'manifest.txt'))); "
            + "$parsed = Convert-HandlerOrderNames (ConvertFrom-HandlerOrderText ([IO.File]::ReadAllText((Join-Path $PWD 'reference.txt')))) $renames; "
            + "foreach ($name in $parsed['mod_load'].action) { 'NAME ' + $name }";
        var (exitCode, output) = RunPowerShell(command);

        exitCode.Should().Be(0, output);
        var mapped = output.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("NAME ", StringComparison.Ordinal))
            .Select(line => line["NAME ".Length..])
            .ToList();
        mapped.Should().Equal(splits.SelectMany(names => names.Skip(1)), "each split handler becomes its parts, where master's one handler stood");
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
