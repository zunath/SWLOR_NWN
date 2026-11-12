using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ModuleHakBuildTests
{
    [Test]
    public void EveryModuleHakIsBuiltByTheDeploy()
    {
        var root = FindRepositoryRoot();
        // Module JSON stores accented text as single Windows-1252 bytes; hak names are ASCII.
        using var module = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root.FullName, "Module", "ifo", "module.ifo.json"), Encoding.Latin1));
        using var build = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root.FullName, "Build", "hakbuilder.json")));

        var moduleHaks = module.RootElement.GetProperty("Mod_HakList").GetProperty("value").EnumerateArray()
            .Select(entry => entry.GetProperty("Mod_Hak").GetProperty("value").GetString())
            .ToList();
        var builtHaks = build.RootElement.GetProperty("HakList").EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object)
            .Select(entry => entry.GetProperty("Name").GetString())
            .ToHashSet();

        moduleHaks.Should().NotBeEmpty();
        moduleHaks.Where(hak => !builtHaks.Contains(hak)).Should().BeEmpty(
            "the deploy builds HAKs from Build/hakbuilder.json, and NWSync fails on a module HAK it cannot find");
    }

    [TestCase("sw_anim_f")]
    [TestCase("sw_anim_f1")]
    [TestCase("sw_anim_m")]
    [TestCase("sw_anim_m1")]
    [TestCase("sw_cr_creature")]
    [TestCase("sw_cr_creature1")]
    public void RifleAnimationArchivesFitTheNativeSignedOffsetBudget(string directory)
    {
        var haks = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks");
        // The full Git index prevents a sparse checkout from hiding large inputs.
        // Use current files when present, including staged relocations and edits.
        var committedSizes = RunGit(haks, "ls-tree", "-rl", "HEAD", "--", directory)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line[..line.IndexOf('\t')].Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(fields => fields[2])
            .ToDictionary(group => group.Key, group => long.Parse(group.First()[3], CultureInfo.InvariantCulture));
        var entries = RunGit(haks, "ls-files", "--stage", "-z", "--", directory)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        entries.Should().NotBeEmpty("each configured rifle archive must own source resources");
        long bytes = 160;
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf('\t');
            var fields = entry[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            fields[2].Should().Be("0", "unmerged assets cannot produce a valid archive");
            var file = Path.Combine(haks, entry[(separator + 1)..]);
            var length = File.Exists(file) ? new FileInfo(file).Length : committedSizes[fields[1]];
            bytes = checked(bytes + length + 32 + 3); // ERF tables and conservative alignment.
        }
        bytes.Should().BeLessThan(2L * 1024 * 1024 * 1024,
            "native model loading uses signed archive offsets; overflow resources need another module HAK");
    }

    private static string RunGit(string haks, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = haks,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, error);
        return output;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")) &&
                File.Exists(Path.Combine(directory.FullName, "Build", "hakbuilder.json")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
