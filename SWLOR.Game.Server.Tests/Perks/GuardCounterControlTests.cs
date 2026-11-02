using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Guard Counter III's guarded Daze must be the impact's own status, like Joint Lock's Daze, so
/// Control-keyed Katar perks (Scrapper Control, Guard Training, Redirecting Guard) count it.
/// </summary>
public class GuardCounterControlTests
{
    [Test]
    public void GuardCounterIII_DazesAsItsImpactStatusAfterAGuardedHit()
    {
        var profile = GetProfile(new GuardCounterAbilityDefinition().BuildAbilities()[FeatType.GuardCounter3]);

        GetProperty<Type>(profile, "RecentGuardedHitStatusEffect").Should().Be(typeof(DazedStatusEffect));
        GetProperty<int>(profile, "RecentGuardedHitStatusDurationSeconds").Should().Be(15);
        GetProperty<float>(profile, "RecentGuardedHitWindowSeconds").Should().Be(30f);
        GetProperty<Type>(profile, "ConditionalTargetStatusEffect").Should().BeNull(
            "an after-hit conditional status bypasses the impact's status categories");
        ((new DazedStatusEffect().Categories & StatusEffectCategory.Control) != 0).Should().BeTrue(
            "Control-keyed perks recognize the guarded Daze through its category");
    }

    [TestCase(FeatType.GuardCounter1)]
    [TestCase(FeatType.GuardCounter2)]
    public void GuardCounterIAndII_InflictNoStatus(FeatType feat)
    {
        var profile = GetProfile(new GuardCounterAbilityDefinition().BuildAbilities()[feat]);

        GetProperty<Type>(profile, "RecentGuardedHitStatusEffect").Should().BeNull();
        GetProperty<Type>(profile, "ConditionalTargetStatusEffect").Should().BeNull();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void GuardedHitStatus_RejectsAnAuthoredImpactStatusOrFriendlyAbility(bool authoredStatus, bool friendly)
    {
        var configure = () => ConfigureProbe.Configure(authoredStatus ? typeof(SunderStatusEffect) : null, !friendly);

        configure.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void GuardedHitStatus_ConfiguresOnAHostileAbilityWithoutItsOwnStatus()
    {
        var configure = () => ConfigureProbe.Configure(null, true);

        configure.Should().NotThrow();
    }

    [Test]
    public void BreakerReversal_RequiresAControlAbility()
    {
        // Building the perk list needs the engine, so read the generated definition instead.
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;
        var source = File.ReadAllText(Path.Combine(
            directory!.FullName, "SWLOR.Game.Server", "Feature", "PerkDefinition", "KatarPerkDefinition.cs"));
        var start = source.IndexOf("private void BreakerReversal()", StringComparison.Ordinal);
        var breakerReversal = source[start..source.IndexOf("private void IronGrip()", start, StringComparison.Ordinal)];

        breakerReversal.Should().Contain("your next control ability");
        breakerReversal.Should().Contain(
            ".IncreasesStat(StatType.DamageTakenNextSkillAbilityRequiredCategory, (int)StatusEffectCategory.Control)",
            "the description promises the bonus to the next control ability, not any Katar ability");
    }

    private sealed class ConfigureProbe : WeaponActiveAbilityDefinitionBase
    {
        public static void Configure(Type statusEffect, bool isHostile)
        {
            ConfigureWeaponAbility(
                new AbilityBuilder().Create(FeatType.GuardCounter3, PerkType.GuardCounter).Name("Probe").Level(3),
                SkillType.Katar,
                28,
                statusEffect == null ? 0 : 15,
                statusEffect,
                null,
                8,
                0,
                0.0f,
                false,
                isHostile,
                false,
                Spell.Invalid,
                AbilityTargetingShapeType.None,
                0.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new WeaponAbilityProfile
                {
                    IsQueuedWeaponAbility = true,
                    RecentGuardedHitWindowSeconds = 30,
                    RecentGuardedHitStatusEffect = typeof(DazedStatusEffect),
                    RecentGuardedHitStatusDurationSeconds = 15
                });
        }
    }

    private static object GetProfile(AbilityDetail ability) =>
        ability.ImpactAction.Target!.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(field => field.FieldType.Name == "WeaponAbilityProfile")
            .GetValue(ability.ImpactAction.Target)!;

    private static T GetProperty<T>(object profile, string name) =>
        (T)profile.GetType().GetProperty(name)!.GetValue(profile)!;
}
