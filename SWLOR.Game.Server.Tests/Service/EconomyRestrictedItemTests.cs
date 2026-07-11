using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class EconomyRestrictedItemTests
{
    [Test]
    public void IsEconomyRestrictedName_FlagsReservedNpcPrefixes()
    {
        Item.IsEconomyRestrictedName("[NPC] Mandalorian Armor").Should().BeTrue();
        Item.IsEconomyRestrictedName("(NPC 2) Sith Lightsaber").Should().BeTrue();
        Item.IsEconomyRestrictedName("  [NPC] Padded leading whitespace").Should().BeTrue();
        Item.IsEconomyRestrictedName("[npc] lowercase variant").Should().BeTrue();
    }

    [Test]
    public void IsEconomyRestrictedName_FlagsBlankNames()
    {
        Item.IsEconomyRestrictedName(null).Should().BeTrue();
        Item.IsEconomyRestrictedName("").Should().BeTrue();
        Item.IsEconomyRestrictedName("   ").Should().BeTrue();
    }

    [Test]
    public void IsEconomyRestrictedName_AllowsOrdinaryPlayerItems()
    {
        Item.IsEconomyRestrictedName("Basic Longsword").Should().BeFalse();
        Item.IsEconomyRestrictedName("Lightsaber").Should().BeFalse();
        Item.IsEconomyRestrictedName("Warocas Leg").Should().BeFalse();
        Item.IsEconomyRestrictedName("DNA: Warocas").Should().BeFalse();
        // "NPC" appearing mid-name must not trip the anchored prefix match.
        Item.IsEconomyRestrictedName("Datapad on NPC Movements").Should().BeFalse();
    }

    /// <summary>
    /// Guards that the reserved NPC name prefixes stay aligned with the module's actual naming
    /// convention: every item blueprint the builders named with an [NPC]/(NPC prefix must be flagged
    /// by the shared classifier. If a new convention is introduced this test fails, prompting the
    /// classifier (and the economy-exclusion rule) to be updated.
    /// </summary>
    [Test]
    public void EveryNpcPrefixedBlueprint_IsFlaggedByTheClassifier()
    {
        var utiDirectory = Path.Combine(FindRepositoryRoot().FullName, "Module", "uti");
        var offenders = new List<string>();
        var checkedCount = 0;

        foreach (var file in Directory.EnumerateFiles(utiDirectory, "*.uti.json"))
        {
            var name = ReadBlueprintName(file);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var trimmed = name.TrimStart();
            if (!trimmed.StartsWith("[NPC]") && !trimmed.StartsWith("(NPC"))
                continue;

            checkedCount++;
            if (!Item.IsEconomyRestrictedName(name))
                offenders.Add($"{Path.GetFileName(file)}: '{name}'");
        }

        checkedCount.Should().BeGreaterThan(0, "the module should contain NPC-prefixed blueprints to validate against");
        offenders.Should().BeEmpty("every NPC-prefixed blueprint must be economy-restricted:\n" + string.Join("\n", offenders));
    }

    private static string ReadBlueprintName(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        if (!document.RootElement.TryGetProperty("LocalizedName", out var localizedName) ||
            !localizedName.TryGetProperty("value", out var value) ||
            !value.TryGetProperty("0", out var firstEntry))
        {
            return null;
        }

        return firstEntry.GetString();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
