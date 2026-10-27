using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Example of testing game code that calls an NWNX plugin: the plugin is swapped for a recording fake, so the test sees exactly
/// which plugin calls the game function made, and no game engine is needed.
/// </summary>
public class QuickCastEngineTests
{
    private const uint Creature = 1001;

    private static (FeatType Alias, FeatType Original) AnAutomaticTargetingFeat()
    {
        var (alias, original) = QuickCastFeatMap.Originals.First();
        return (alias, original);
    }

    /// <summary>Checks that a creature knowing the ordinary feat is found with one plugin call.</summary>
    [Test]
    public void KnowsFeat_StopsAtTheOriginalFeatWhenTheCreatureKnowsIt()
    {
        var (alias, original) = AnAutomaticTargetingFeat();
        var plugin = new FakeCreaturePlugin();
        plugin.SetReturn("GetKnowsFeat", true);
        using var scope = CreaturePluginRuntime.Override(plugin);

        QuickCast.KnowsFeat(Creature, alias).Should().BeTrue();

        plugin.Calls.Select(call => call.ToString()).Should().Equal($"GetKnowsFeat(1001, {original})");
    }

    /// <summary>Checks that a creature that only has the automatic-targeting feat still counts as knowing the ability.</summary>
    [Test]
    public void KnowsFeat_FallsBackToTheAutomaticTargetingFeat()
    {
        var (alias, original) = AnAutomaticTargetingFeat();
        var plugin = new FakeCreaturePlugin();
        plugin.SetReturnHandler("GetKnowsFeat", arguments => (FeatType)arguments[1]! == alias);
        using var scope = CreaturePluginRuntime.Override(plugin);

        QuickCast.KnowsFeat(Creature, original).Should().BeTrue();

        plugin.Calls.Select(call => call.ToString()).Should().Equal($"GetKnowsFeat(1001, {original})", $"GetKnowsFeat(1001, {alias})");
    }

    /// <summary>Checks that a creature without either feat does not know the ability.</summary>
    [Test]
    public void KnowsFeat_IsFalseWhenTheCreatureKnowsNeitherFeat()
    {
        var (_, original) = AnAutomaticTargetingFeat();
        using var scope = CreaturePluginRuntime.Override(new FakeCreaturePlugin());

        QuickCast.KnowsFeat(Creature, original).Should().BeFalse();
    }

    /// <summary>Checks that removing an ability takes away both the ordinary feat and its automatic-targeting twin.</summary>
    [Test]
    public void RemoveFeat_RemovesTheOriginalAndTheAutomaticTargetingFeat()
    {
        var (alias, original) = AnAutomaticTargetingFeat();
        var plugin = new FakeCreaturePlugin();
        using var scope = CreaturePluginRuntime.Override(plugin);

        QuickCast.RemoveFeat(Creature, alias);

        plugin.Calls.Select(call => call.ToString()).Should().Equal($"RemoveFeat(1001, {original})", $"RemoveFeat(1001, {alias})");
    }

    /// <summary>Checks that a feat without an automatic-targeting twin is removed with a single plugin call.</summary>
    [Test]
    public void RemoveFeat_RemovesOnlyTheFeatWhenItHasNoTwin()
    {
        var feat = Enum.GetValues<FeatType>().First(f => !QuickCastFeatMap.IsAlias(f) && !QuickCastFeatMap.TryGetAlias(f, out _));
        var plugin = new FakeCreaturePlugin();
        using var scope = CreaturePluginRuntime.Override(plugin);

        QuickCast.RemoveFeat(Creature, feat);

        plugin.Calls.Select(call => call.ToString()).Should().Equal($"RemoveFeat(1001, {feat})");
    }
}
