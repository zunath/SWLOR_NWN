using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The shared machinery the space abilities fire weapons through: which weapon fires, the next-weapon-hit rider,
/// the space radii and the combat events.
/// </summary>
public class SpaceAbilityEngineTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SpaceWeaponCandidate Weapon(
        int slot, int rank, SpaceWeaponFamily family, float range = 40f, bool usable = true) =>
        new() { Slot = slot, Rank = rank, Family = family, Range = range, Usable = usable };

    [Test]
    public void Pick_TakesTheHighestRankedUsableWeaponOfTheFamily()
    {
        var weapons = new[]
        {
            Weapon(1, 10, SpaceWeaponFamily.Energy),
            Weapon(2, 30, SpaceWeaponFamily.Energy),
            Weapon(3, 45, SpaceWeaponFamily.Launcher),
            Weapon(4, 20, SpaceWeaponFamily.Energy),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 20f).Slot.Should().Be(2);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Launcher, 20f).Slot.Should().Be(3);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Any, 20f).Slot.Should().Be(3);
    }

    [Test]
    public void Pick_BreaksATieWithTheLowerSlot()
    {
        var weapons = new[]
        {
            Weapon(7, 25, SpaceWeaponFamily.Energy),
            Weapon(3, 25, SpaceWeaponFamily.Energy),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 10f).Slot.Should().Be(3);
    }

    [Test]
    public void Pick_SkipsUnusableWeaponsAndWeaponsThatCannotReach()
    {
        var weapons = new[]
        {
            Weapon(1, 40, SpaceWeaponFamily.Energy, range: 40f, usable: false),
            Weapon(2, 30, SpaceWeaponFamily.Energy, range: 15f),
            Weapon(3, 10, SpaceWeaponFamily.Energy, range: 40f),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 30f).Slot.Should().Be(3);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 10f).Slot.Should().Be(2);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 50f).Should().BeNull();
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Launcher, 10f).Should().BeNull();
    }

    [Test]
    public void LongestRange_ReadsOnlyUsableWeaponsOfTheFamily()
    {
        var weapons = new[]
        {
            Weapon(1, 10, SpaceWeaponFamily.Energy, range: 40f),
            Weapon(2, 10, SpaceWeaponFamily.Energy, range: 60f, usable: false),
            Weapon(3, 10, SpaceWeaponFamily.Launcher, range: 50f),
        };

        SpaceWeaponPicker.LongestRange(weapons, SpaceWeaponFilter.Energy).Should().Be(40f);
        SpaceWeaponPicker.LongestRange(weapons, SpaceWeaponFilter.Any).Should().Be(50f);
        SpaceWeaponPicker.LongestRange(Array.Empty<SpaceWeaponCandidate>(), SpaceWeaponFilter.Any).Should().Be(0f);
    }

    [Test]
    public void IsSeatUsable_AllowsTheHelmAndCrewedStationsOnly()
    {
        SpaceWeaponPicker.IsSeatUsable(ShipSlotTag.Helm, false).Should().BeTrue();
        SpaceWeaponPicker.IsSeatUsable(null, false).Should().BeTrue("an older ship has no tags");
        SpaceWeaponPicker.IsSeatUsable(ShipSlotTag.Gunner, false).Should().BeFalse();
        SpaceWeaponPicker.IsSeatUsable(ShipSlotTag.Gunner, true).Should().BeTrue();
    }

    [Test]
    public void CrewStubs_AreNeutralUntilTheCrewUpdateFillsThemIn()
    {
        SpaceCrew.EffectivenessMultiplier(0, 0).Should().Be(1.0);
        SpaceCrew.IsSeatCrewed(new ShipStatus(), 1).Should().BeFalse();
    }

    // Riders

    [Test]
    public void Rider_MatchesOnlyBeforeItLapsesAndOnlyForItsFamily()
    {
        var rider = new SpaceShotRider { Key = "ram", Filter = SpaceWeaponFilter.Energy, ExpiresAt = Now.AddSeconds(30) };

        rider.Matches(SpaceWeaponFamily.Energy, Now).Should().BeTrue();
        rider.Matches(SpaceWeaponFamily.Launcher, Now).Should().BeFalse();
        rider.Matches(SpaceWeaponFamily.Energy, Now.AddSeconds(30)).Should().BeFalse("it lapses at the expiry time");
        new SpaceShotRider { Key = "any", ExpiresAt = Now.AddSeconds(1) }
            .Matches(SpaceWeaponFamily.Launcher, Now).Should().BeTrue();
        new SpaceShotRider { Key = "unset" }.Matches(SpaceWeaponFamily.Energy, Now).Should().BeFalse();
    }

    [Test]
    public void ShipStatus_PendingRidersSurviveTheDatabaseRoundTrip()
    {
        var status = new ShipStatus();
        status.PendingRiders.Add(new SpaceShotRider
        {
            Key = "ram",
            BonusDmg = 10,
            ExtraDamage = 24,
            ExtraEnmity = 350,
            Amount = 4,
            Filter = SpaceWeaponFilter.Launcher,
            ExpiresAt = Now,
        });

        var copy = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status));

        copy.PendingRiders.Should().ContainSingle();
        var rider = copy.PendingRiders[0];
        rider.Key.Should().Be("ram");
        rider.BonusDmg.Should().Be(10);
        rider.ExtraDamage.Should().Be(24);
        rider.ExtraEnmity.Should().Be(350);
        rider.Amount.Should().Be(4);
        rider.Filter.Should().Be(SpaceWeaponFilter.Launcher);
        rider.ExpiresAt.Should().Be(Now);
    }

    [Test]
    public void ShipStatus_SavedBeforeRidersExistedLoadsWithAnEmptyList()
    {
        var copy = JsonConvert.DeserializeObject<ShipStatus>("{\"ItemTag\":\"x\",\"Hull\":5}");

        copy.PendingRiders.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void WithBonusDmg_CopiesTheShotSoASharedShotKeepsItsDmg()
    {
        var shot = new SpaceWeaponShot { Dmg = 12, Rank = 5, Family = SpaceWeaponFamily.Launcher, IsAbility = true };

        var boosted = shot.WithBonusDmg(8);

        boosted.Dmg.Should().Be(20);
        boosted.Rank.Should().Be(5);
        boosted.Family.Should().Be(SpaceWeaponFamily.Launcher);
        boosted.IsAbility.Should().BeTrue();
        shot.Dmg.Should().Be(12);
    }

    [Test]
    public void AShotConsumesRidersUnlessItBelongsToAnAbility()
    {
        new SpaceWeaponShot().ConsumesRiders.Should().BeTrue("a weapon cycle is the next weapon hit");
        new SpaceFireOptions().ConsumesRiders.Should().BeFalse("an ability leaves riders for the next weapon cycle");
        new SpaceFireOptions().IsAbility.Should().BeTrue();
    }

    // Weapons

    /// <summary>
    /// Every catalog step that deals damage is a weapon FireStep knows how to fire, so a new weapon family cannot
    /// be fitted to a hull and be unusable by abilities.
    /// </summary>
    [Test]
    public void EveryDamagingCatalogStep_IsAFireableWeapon()
    {
        var damaging = RevampModuleCatalog.Steps
            .Where(x => x.Dmg > 0 && x.WeaponFamily != 0)
            .Select(x => x.Family)
            .Distinct()
            .ToList();

        damaging.Should().NotBeEmpty();
        damaging.Where(x => !ShipModuleActions.IsShipWeapon(x)).Should().BeEmpty();
    }

    [Test]
    public void EveryShipWeaponFamily_HasStepsWithAWeaponFamilyAndOnlyLaunchersNeedAmmo()
    {
        foreach (var family in Enum.GetValues<ShipModuleFamily>().Where(ShipModuleActions.IsShipWeapon))
        {
            var steps = RevampModuleCatalog.Steps.Where(x => x.Family == family).ToList();
            steps.Should().NotBeEmpty($"{family} is a weapon family");
            steps.Should().OnlyContain(x => x.WeaponFamily != 0 && x.Activation != ShipModuleActivation.Invalid);

            var ammo = ShipModuleActions.GetAmmoResref(family);
            if (ammo != null)
                steps.Should().OnlyContain(x => x.WeaponFamily == SpaceWeaponFamily.Launcher, $"{family} uses ammo");
        }
    }

    // Radii and auras

    [Test]
    public void SpaceRadii_ScaleTheGroundAreaByOneFactor()
    {
        SpaceRadii.GroundToSpaceFactor.Should().Be(4f);
        SpaceRadii.GroundAreaMeters.Should().Be(5f);
        SpaceRadii.NearShip.Should().Be(20f);
        SpaceRadii.NearTarget.Should().Be(20f);
        SpaceRadii.FromGround(3f).Should().Be(12f);
        SpaceRadii.CommandRange.Should().Be(30f);
    }

    [Test]
    public void CommandRange_AddsTheScaledGroundBonusAndAnyExtraMeters()
    {
        SpaceRadii.GetCommandRange(0).Should().Be(30f);
        SpaceRadii.GetCommandRange(2).Should().Be(38f);
        SpaceRadii.GetCommandRange(4, 5f).Should().Be(51f);
        SpaceRadii.GetCommandRange(-3, -5f).Should().Be(30f, "penalties never shrink the base range");
    }

    [TestCase(8, 1)]
    [TestCase(10, 1)]
    [TestCase(15, 2)]
    [TestCase(20, 3)]
    [TestCase(25, 4)]
    [TestCase(60, 4)]
    public void MaxAuras_FollowsTheGroundSocialRule(int social, int expected)
    {
        SpaceAuraRules.MaxAuras(social).Should().Be(expected);
    }

    [Test]
    public void Reconcile_GivesTheAuraToNewShipsAndTakesItFromShipsThatLeft()
    {
        var (add, remove) = SpaceAuraRules.Reconcile(new uint[] { 1, 2, 3 }, new uint[] { 2, 3, 4, 5 });

        add.Should().BeEquivalentTo(new uint[] { 4, 5 });
        remove.Should().BeEquivalentTo(new uint[] { 1 });
        var (noAdd, noRemove) = SpaceAuraRules.Reconcile(new uint[] { 1 }, new uint[] { 1 });
        noAdd.Should().BeEmpty();
        noRemove.Should().BeEmpty();
    }

    [Test]
    public void HasEqualOrStrongerCopy_StopsTwoCommandersReplacingEachOther()
    {
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 14 }).Should().BeTrue();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 16 }).Should().BeTrue();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 12 }).Should().BeFalse();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, Array.Empty<int>()).Should().BeFalse();
    }

    // Events

    [Test]
    public void CombatEvents_ReachEverySubscriberEvenWhenOneThrows()
    {
        var seen = new List<string>();
        Action<SpaceShotEvent> failing = _ => throw new InvalidOperationException("handler bug");
        Action<SpaceShotEvent> recording = e => seen.Add($"landed {e.Result.Damage}");
        SpaceCombatEvents.ShotLanded += failing;
        SpaceCombatEvents.ShotLanded += recording;
        try
        {
            SpaceCombatEvents.RaiseShotLanded(new SpaceShotEvent
            {
                Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Launcher, IsAbility = true },
                Result = new ShipShotResult { IsHit = true, Damage = 31 },
            });
        }
        finally
        {
            SpaceCombatEvents.ShotLanded -= failing;
            SpaceCombatEvents.ShotLanded -= recording;
        }

        seen.Should().Equal("landed 31");
    }

    [Test]
    public void ShotEvent_ReportsTheFamilyAndWhetherAnAbilityFiredIt()
    {
        var abilityShot = new SpaceShotEvent { Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Launcher, IsAbility = true } };
        var cycleShot = new SpaceShotEvent { Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Energy } };

        abilityShot.Family.Should().Be(SpaceWeaponFamily.Launcher);
        abilityShot.FromAbility.Should().BeTrue();
        cycleShot.Family.Should().Be(SpaceWeaponFamily.Energy);
        cycleShot.FromAbility.Should().BeFalse();
    }

    [Test]
    public void RaisingWithNoSubscribersDoesNothing()
    {
        var raise = () =>
        {
            SpaceCombatEvents.RaiseShotEvaded(new SpaceShotEvent());
            SpaceCombatEvents.RaiseHullDamaged(new SpaceHullEvent());
            SpaceCombatEvents.RaiseHostileAbilityUsed(new SpaceAbilityEvent());
        };

        raise.Should().NotThrow();
    }

    [Test]
    public void FireResult_FailureCarriesTheReasonAndNeverCountsAsAHit()
    {
        var result = SpaceFireResult.Failure("Your ship has no energy weapon fitted.");

        result.Fired.Should().BeFalse();
        result.IsHit.Should().BeFalse();
        result.IsCritical.Should().BeFalse();
        result.Error.Should().Be("Your ship has no energy weapon fitted.");
    }

    [Test]
    public void FireResult_ReportsHitAndCriticalFromTheShot()
    {
        var result = new SpaceFireResult { Fired = true, Shot = new ShipShotResult { IsHit = true, IsCritical = true, Damage = 40 } };

        result.IsHit.Should().BeTrue();
        result.IsCritical.Should().BeTrue();
    }
}
