using System.IO;
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
