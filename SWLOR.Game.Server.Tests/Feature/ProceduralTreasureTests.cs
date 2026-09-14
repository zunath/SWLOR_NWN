using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Feature;

public class ProceduralTreasureTests
{
    [Test]
    public void OpenHandler_FillsConfiguredLootOnlyOnce()
    {
        ScriptName.OnProceduralTreasureOpened.Should().Be("proc_loot_open");

        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ProceduralTreasure.cs"));
        var guard = source.IndexOf("GetLocalBool(container, FilledVariable)", StringComparison.Ordinal);
        var markFilled = source.IndexOf("SetLocalBool(container, FilledVariable, true)", StringComparison.Ordinal);
        var spawn = source.IndexOf("Loot.SpawnLoot(container, container, \"LOOT_TABLE_\")", StringComparison.Ordinal);

        guard.Should().BeGreaterThanOrEqualTo(0);
        markFilled.Should().BeGreaterThan(guard);
        spawn.Should().BeGreaterThan(markFilled,
            "the container must be marked before loot generation so a repeated open cannot duplicate rewards");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
