using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Two perks that each author the same buff window share one duration stat. Those stats are read
/// as a single aggregate, so owning both perks must not double the authored window.
/// </summary>
public class IndependentBuffWindowTests
{
    private static readonly (StatType Stat, string Reason)[] AuthoredWindows =
    {
        (StatType.StatusAppliedSelfDurationSeconds,
            "Guard Training and Redirecting Guard each author a 30-second self buff"),
        (StatType.AbilityRestoredFPHasteDurationSeconds,
            "Energized Forms and Flow of the Maelstrom each author 30 seconds of Haste"),
        (StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds,
            "Soul Ascension and Soul Sacrifice each author a 30-second HP-spend window")
    };

    [TestCaseSource(nameof(AuthoredWindows))]
    public void SharedAuthoredWindow_UsesTheLongestWindowRatherThanTheSum((StatType Stat, string Reason) window)
    {
        Stat.GetStatTypeAggregation(window.Stat)
            .Should().Be(StatTypeAggregation.Maximum, window.Reason);
        Stat.AggregateStatAdjustment(window.Stat, 30, 30)
            .Should().Be(30, window.Reason);
    }

    [Test]
    public void KatarScrapperSelfBuffs_BothAuthorThirtySeconds()
    {
        Window<KatarPerkDefinition>("GuardTraining", PerkType.GuardTraining).Should().Be(30);
        Window<KatarPerkDefinition>("RedirectingGuard", PerkType.RedirectingGuard).Should().Be(30);
    }

    [Test]
    public void SaberstaffFPHasteBuffs_BothAuthorThirtySeconds()
    {
        WindowOf<SaberstaffPerkDefinition>("EnergizedForms", PerkType.EnergizedForms,
            StatType.AbilityRestoredFPHasteDurationSeconds).Should().Be(30);
        WindowOf<SaberstaffPerkDefinition>("FlowOfTheMaelstrom", PerkType.FlowOfTheMaelstrom,
            StatType.AbilityRestoredFPHasteDurationSeconds).Should().Be(30);
    }

    [Test]
    public void HeavyVibrobladeHitPointSpendBuffs_BothAuthorThirtySeconds()
    {
        WindowOf<HeavyVibrobladePerkDefinition>("SoulAscension", PerkType.SoulAscension,
            StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds).Should().Be(30);
        WindowOf<HeavyVibrobladePerkDefinition>("SoulSacrifice", PerkType.SoulSacrifice,
            StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds).Should().Be(30);
    }

    private static int Window<T>(string method, PerkType type) where T : new() =>
        WindowOf<T>(method, type, StatType.StatusAppliedSelfDurationSeconds);

    private static int WindowOf<T>(string method, PerkType type, StatType stat) where T : new()
    {
        var definition = new T();
        typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(definition, null);
        var builder = typeof(T).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        var maxLevel = perks[type].PerkLevels.OrderByDescending(x => x.Key).First().Value;
        return maxLevel.StatBonuses.Where(x => x.Stat == stat).Sum(x => x.Calculate(0));
    }
}
