using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Keeps the locked systems reading time through <c>Clock</c>. Code merged from master that reads the system clock
/// directly in a listed file fails here; <c>tools/Refactor/Apply-ClockSeam.ps1</c> fixes it.
/// </summary>
[TestFixture]
public class ClockSeamSourceGuardTests
{
    private static readonly string[] LockedSystemEntries =
    {
        "Service/Combat.cs",
        "Service/CombatService/**",
        "Service/CombatPoint.cs",
        "Service/OrdinaryAttackTraits.cs",
        "Service/PairedRangedTraits.cs",
        "Service/StatusEffect.cs",
        "Service/StatusEffectService/**",
        "Service/Stat.cs",
        "Service/Perk.cs",
        "Service/AbilityService/**",
        "Service/Craft.cs",
        "Native/PairedRangedPresentation.cs",
        "Native/OnAIActionAttackObject.cs",
    };

    [Test]
    public void ManifestListsTheLockedSystemsAndEveryEntryExists()
    {
        var root = ClockSeamSources.FindRepositoryRoot();

        ClockSeamSources.ReadEntries(root).Should().Contain(LockedSystemEntries);
        ClockSeamSources.ListFiles(root).Should().HaveCountGreaterThan(LockedSystemEntries.Length);
    }

    [Test]
    public void ListedFilesReadTimeOnlyThroughClock()
    {
        var root = ClockSeamSources.FindRepositoryRoot();

        var directReads = new List<string>();
        foreach (var file in ClockSeamSources.ListFiles(root))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            foreach (var (line, text) in ClockSeamSources.FindDirectReads(File.ReadAllText(file)))
                directReads.Add($"{relative}:{line} {text}");
        }

        directReads.Should().BeEmpty(
            "these files must read time through SWLOR.Game.Server.Core.Clock; run tools/Refactor/Apply-ClockSeam.ps1");
    }

    [Test]
    public void ProductionCodeNeverCallsTheClockOverride()
    {
        var root = ClockSeamSources.FindRepositoryRoot();
        var serverRoot = Path.Combine(root, "SWLOR.Game.Server");
        var clockFile = Path.Combine(serverRoot, "Core", "Clock.cs");

        var callers = Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(serverRoot, file) && !string.Equals(file, clockFile, StringComparison.OrdinalIgnoreCase))
            .Where(file => File.ReadAllText(file).Contains("OverrideForTests", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .ToList();

        callers.Should().BeEmpty("only tests may override the clock, and a scope left active would freeze the server's time");
    }

    [Test]
    public void DirectReadsAreFoundInCodeButNotInCommentsStringsOrOtherTypes()
    {
        const string source = """
            using System;
            class Sample
            {
                // DateTime.UtcNow in a comment
                string text = "Environment.TickCount64 in a string";
                void Run()
                {
                    var a = DateTime.UtcNow;
                    var b = System.DateTime.UtcNow.AddSeconds(1);
                    var c = global::System.DateTime.UtcNow;
                    var d = Environment.TickCount64;
                    var e = System.Environment.TickCount64;
                    var e2 = global::System.Environment.TickCount64;
                    var f = Clock.UtcNow;
                    var g = Clock.TickCount64;
                    var h = DateTimeOffset.UtcNow;
                    var i = Other.DateTime.UtcNow;
                    var j = DateTime.Now;
                }
            }
            """;

        var reads = ClockSeamSources.FindDirectReads(source);

        reads.Select(read => read.Line).Should().Equal(8, 9, 10, 11, 12, 13);
    }

    private static bool IsBuildOutput(string serverRoot, string file)
    {
        var relative = Path.GetRelativePath(serverRoot, file).Replace('\\', '/');
        return relative.StartsWith("bin/", StringComparison.Ordinal) || relative.StartsWith("obj/", StringComparison.Ordinal);
    }
}
