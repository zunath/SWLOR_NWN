#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DroidInstructionMigrationTests
{
    [Test]
    public void HistoricalInstructionsDoNotBecomeDifferentAbilitiesWhenAnIdIsReused()
    {
        var method = typeof(_22_CombatSystemReplacement).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ObsoleteItemMigration")!
            .GetMethod("RemoveReassignedDroidPerks", BindingFlags.NonPublic | BindingFlags.Static)!;
        var perks = new List<DroidPerk> { new((PerkType)23, 1), new(PerkType.MedKit, 2) };

        method.Invoke(null, new object[] { perks }).Should().Be(true);
        perks.Should().ContainSingle().Which.Perk.Should().Be(PerkType.MedKit);
        perks.Single().Level.Should().Be(2);
        method.Invoke(null, new object[] { perks }).Should().Be(false);
    }

    [Test]
    public void MissingHistoricalInstructionListNeedsNoChange()
    {
        var method = typeof(_22_CombatSystemReplacement).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ObsoleteItemMigration")!
            .GetMethod("RemoveReassignedDroidPerks", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object?[] { null }).Should().Be(false);
    }
}
