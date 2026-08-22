using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneEnemyAppearanceTests
{
    // package -> its three line codes; enemies are cp_<line>_<ad|sp|wd|ic|ms>.
    private static readonly string[][] Packages =
    {
        new[] { "invinc", "vitrupt", "sysshut" },
        new[] { "killbeacon", "embunker", "deccommand" },
        new[] { "sabstorm", "guardmst", "sabcycl" },
        new[] { "emcocktail", "holdline", "infconduit" },
        new[] { "absdef", "soulasc", "forcebane" },
        new[] { "lightstand", "darkhung", "eclipse" },
        new[] { "adamguard", "scraplock", "worldbrk" },
        new[] { "unmovctr", "lastword", "deadhand" },
        new[] { "killbox", "oneshot", "rainsteel" },
        new[] { "thermdet", "overbarr", "perflurry" },
        new[] { "cripdef", "tempbloom", "redbloom" },
        new[] { "primover", "untinst", "forcebeast" },
        new[] { "apexbite", "unbrbeast", "alpharhy" },
    };

    private static readonly string[] Tiers = { "ad", "sp", "wd", "ic", "ms" };

    [Test]
    public void CapstoneEnemies_DoNotAllShareTheSameAppearance()
    {
        var appearances = AllEnemies().Select(GetAppearance).ToList();

        appearances.Should().HaveCount(195);
        appearances.Distinct().Should().HaveCountGreaterThanOrEqualTo(20,
            "capstone enemies must not reuse a couple of placeholder appearances");

        // No single appearance may blanket a large share of the roster (the old 974/2154 problem).
        foreach (var group in appearances.GroupBy(a => a))
        {
            group.Count().Should().BeLessThanOrEqualTo(40,
                $"appearance {group.Key} is reused by too many enemies");
        }
    }

    [Test]
    public void EachPackage_UsesMoreThanOneAppearance()
    {
        foreach (var lines in Packages)
        {
            var appearances = lines
                .SelectMany(line => Tiers.Select(tier => $"cp_{line}_{tier}"))
                .Select(GetAppearance)
                .Distinct()
                .ToArray();

            appearances.Length.Should().BeGreaterThanOrEqualTo(2,
                $"package [{string.Join(",", lines)}] should vary appearance across tiers");
        }
    }

    [Test]
    public void DistinctPackages_LookDifferentFromEachOther()
    {
        // Each package's dominant (grunt) appearance should be reasonably distinct across packages,
        // so a Sith dungeon does not look identical to a militia dungeon.
        var grunt = Packages
            .Select(lines => GetAppearance($"cp_{lines[0]}_ad"))
            .ToArray();

        grunt.Distinct().Should().HaveCountGreaterThanOrEqualTo(10,
            "packages should not all share one grunt appearance");
    }

    private static IEnumerable<string> AllEnemies() =>
        Packages.SelectMany(lines => lines).SelectMany(line => Tiers.Select(tier => $"cp_{line}_{tier}"));

    private static int GetAppearance(string resref)
    {
        var root = FindRepositoryRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root.FullName, "Module", "utc", $"{resref}.utc.json")));
        return doc.RootElement.GetProperty("Appearance_Type").GetProperty("value").GetInt32();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
