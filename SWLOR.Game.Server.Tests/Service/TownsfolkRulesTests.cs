using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Game.Server.Service.TownsfolkService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A city's street life (Player City Revamp, "Ambient townsfolk"): how many wanderers, guards, and critters each level and
/// switch gives, what a change adds or removes, and the choices that decide where they walk.
/// </summary>
public class TownsfolkRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    // Random sources that always pick the first or the last choice, so each pick is predictable.
    private static int First(int max) => 0;
    private static int Last(int max) => max - 1;

    #region Counts per level

    [TestCase(1, 0)]
    [TestCase(2, 4)]
    [TestCase(3, 6)]
    [TestCase(4, 8)]
    [TestCase(5, 10)]
    public void Wanderers_AreTwoPerLevelFromLevelTwo(int level, int expected)
    {
        TownsfolkRules.GetWandererCount(level).Should().Be(expected);
    }

    [TestCase(1, 0)]
    [TestCase(2, 0)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 4)]
    public void Guards_AreTwoAtLevelThreePlusOnePerLevelAbove(int level, int expected)
    {
        TownsfolkRules.GetGuardCount(level).Should().Be(expected);
    }

    [TestCase(1, 0)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(4, 4)]
    [TestCase(5, 5)]
    public void Critters_AreOnePerLevelFromLevelTwo(int level, int expected)
    {
        TownsfolkRules.GetCritterCount(level).Should().Be(expected);
    }

    [TestCase(-3)]
    [TestCase(0)]
    [TestCase(1)]
    public void ALevelBelowTwo_HasNoStreetLife(int level)
    {
        TownsfolkRules.GetTarget(level, false, false, false, 5).Should().Be(TownsfolkTarget.None);
    }

    [Test]
    public void ALevelAboveTheMaximum_NeverCountsAboveIt()
    {
        TownsfolkRules.GetTarget(99, false, false, false, 5)
            .Should().Be(TownsfolkRules.GetTarget(TownsfolkRules.MaximumCityLevel, false, false, false, 5));
    }

    [Test]
    public void ALevelFiveCity_HasTenTownsfolkAsThePlanCountsThem()
    {
        // The Plan's load check names a Metropolis with 10 townsfolk, alongside its guards and critters.
        TownsfolkRules.GetTarget(5, false, false, false, 8).Wanderers.Should().Be(10);
    }

    #endregion

    #region Switches

    [Test]
    public void WithNoSwitchOn_ACityHasEveryGroup()
    {
        TownsfolkRules.GetTarget(4, false, false, false, 6).Should().Be(new TownsfolkTarget(8, 3, 4));
    }

    [Test]
    public void TheTownsfolkSwitch_HidesWanderersAndGuardsButNotCritters()
    {
        TownsfolkRules.GetTarget(4, true, false, false, 6).Should().Be(new TownsfolkTarget(0, 0, 4));
    }

    [Test]
    public void TheCrittersSwitch_HidesCrittersButNotTownsfolk()
    {
        TownsfolkRules.GetTarget(4, false, true, false, 6).Should().Be(new TownsfolkTarget(8, 3, 0));
    }

    [Test]
    public void BothSwitches_HideEverything()
    {
        TownsfolkRules.GetTarget(5, true, true, false, 6).Should().Be(TownsfolkTarget.None);
    }

    [Test]
    public void AStation_HasTownsfolkAndGuardsButNoCritters()
    {
        TownsfolkRules.GetTarget(3, false, false, true, 4).Should().Be(new TownsfolkTarget(6, 2, 0));
        TownsfolkRules.HasCritters(true).Should().BeFalse();
        TownsfolkRules.HasCritters(false).Should().BeTrue();
    }

    [Test]
    public void ACityWithNoEntrance_HasNowhereToWalkAndSoNoStreetLife()
    {
        TownsfolkRules.GetTarget(5, false, false, false, 0).Should().Be(TownsfolkTarget.None);
    }

    [Test]
    public void TheTotal_AddsTheGroupsAndGetReadsEachOne()
    {
        var target = new TownsfolkTarget(8, 3, 4);

        target.Total.Should().Be(15);
        target.Get(TownsfolkGroup.Wanderer).Should().Be(8);
        target.Get(TownsfolkGroup.Guard).Should().Be(3);
        target.Get(TownsfolkGroup.Critter).Should().Be(4);
        target.Get(TownsfolkGroup.Invalid).Should().Be(0);
    }

    [Test]
    public void ANewCity_HasNeitherSwitchOn_SoOlderRecordsNeedNoMigration()
    {
        var city = new WorldProperty();

        city.TownsfolkHidden.Should().BeFalse();
        city.CrittersHidden.Should().BeFalse();
    }

    #endregion

    #region Changes

    [Test]
    public void ALevelRise_AddsTheDifference()
    {
        var before = TownsfolkRules.GetTarget(2, false, false, false, 5);
        var after = TownsfolkRules.GetTarget(3, false, false, false, 5);

        TownsfolkRules.GetChange(before.Wanderers, after.Wanderers).Should().Be(2);
        TownsfolkRules.GetChange(before.Guards, after.Guards).Should().Be(2, "guards start at Township");
        TownsfolkRules.GetChange(before.Critters, after.Critters).Should().Be(1);
    }

    [Test]
    public void ALevelDrop_RemovesTheDifference()
    {
        var before = TownsfolkRules.GetTarget(5, false, false, false, 5);
        var after = TownsfolkRules.GetTarget(2, false, false, false, 5);

        TownsfolkRules.GetChange(before.Wanderers, after.Wanderers).Should().Be(-6);
        TownsfolkRules.GetChange(before.Guards, after.Guards).Should().Be(-4);
        TownsfolkRules.GetChange(before.Critters, after.Critters).Should().Be(-3);
    }

    [Test]
    public void SwitchingAGroupOff_RemovesAllOfItAndSwitchingItOnAddsItBack()
    {
        var on = TownsfolkRules.GetTarget(4, false, false, false, 5);
        var off = TownsfolkRules.GetTarget(4, true, false, false, 5);

        TownsfolkRules.GetChange(on.Wanderers, off.Wanderers).Should().Be(-8);
        TownsfolkRules.GetChange(on.Guards, off.Guards).Should().Be(-3);
        TownsfolkRules.GetChange(on.Critters, off.Critters).Should().Be(0);
        TownsfolkRules.GetChange(off.Wanderers, on.Wanderers).Should().Be(8);
    }

    [Test]
    public void ACrowdAtItsTarget_NeedsNoChange()
    {
        TownsfolkRules.GetChange(6, 6).Should().Be(0);
    }

    [Test]
    public void NegativeCounts_AreTreatedAsZero()
    {
        TownsfolkRules.GetChange(-2, 3).Should().Be(3);
        TownsfolkRules.GetChange(3, -2).Should().Be(-3);
    }

    [TestCase(10, 4, 4)]
    [TestCase(2, 4, 2)]
    [TestCase(5, 0, 0)]
    [TestCase(-1, 4, 0)]
    [TestCase(3, -2, 0)]
    public void ACrowdArrivesAFewPerBeat(int wanted, int budget, int expected)
    {
        TownsfolkRules.LimitSpawns(wanted, budget).Should().Be(expected);
    }

    [Test]
    public void ALargestCrowd_ArrivesWithinFiveBeats()
    {
        var total = TownsfolkRules.GetTarget(5, false, false, false, 6).Total;

        total.Should().Be(19);
        Math.Ceiling(total / (double)TownsfolkRules.MaximumSpawnsPerBeat).Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region Where a new creature appears

    [Test]
    public void ALookNotYetWorn_IsPickedBeforeAnyIsRepeated()
    {
        var used = new[] { 0, 1, 3 };

        TownsfolkRules.PickLookIndex(5, used, First).Should().Be(2);
        TownsfolkRules.PickLookIndex(5, used, Last).Should().Be(4);
    }

    [Test]
    public void WhenEveryLookIsWorn_AnyIsPicked()
    {
        TownsfolkRules.PickLookIndex(3, new[] { 0, 1, 2 }, Last).Should().Be(2);
    }

    [Test]
    public void AnEmptyLookPool_PicksNothing()
    {
        TownsfolkRules.PickLookIndex(0, Array.Empty<int>(), First).Should().Be(-1);
    }

    [Test]
    public void ANewCreature_AppearsAtAnEntranceAwayFromThePlayers()
    {
        var entrances = new[] { new Vector3(10, 10, 0), new Vector3(40, 10, 0), new Vector3(100, 100, 0) };
        var players = new[] { new Vector3(12, 12, 0) };

        TownsfolkRules.PickSpawnIndex(entrances, players, First).Should().Be(1, "entrance 0 is 2.8 m from the player");
        TownsfolkRules.PickSpawnIndex(entrances, players, Last).Should().Be(2);
    }

    [Test]
    public void WhenEveryEntranceIsNearAPlayer_AnyEntranceWillDo()
    {
        var entrances = new[] { new Vector3(10, 10, 0), new Vector3(14, 10, 0) };
        var players = new[] { new Vector3(12, 10, 0) };

        TownsfolkRules.PickSpawnIndex(entrances, players, Last).Should().Be(1);
    }

    [Test]
    public void WithNoPlayersAndNoEntrances_TheSpawnPickIsStillSafe()
    {
        TownsfolkRules.PickSpawnIndex(new[] { new Vector3(1, 1, 0) }, Array.Empty<Vector3>(), First).Should().Be(0);
        TownsfolkRules.PickSpawnIndex(Array.Empty<Vector3>(), Array.Empty<Vector3>(), First).Should().Be(-1);
    }

    #endregion

    #region Walking

    [Test]
    public void AWanderer_NeverPicksTheEntranceItIsAt()
    {
        for (var current = 0; current < 4; current++)
        {
            for (var roll = 0; roll < 3; roll++)
            {
                var pick = TownsfolkRules.PickDestinationIndex(4, current, max => roll % max);
                pick.Should().NotBe(current);
                pick.Should().BeInRange(0, 3);
            }
        }
    }

    [Test]
    public void AWanderer_CanReachEveryOtherEntrance()
    {
        var reached = Enumerable.Range(0, 3)
            .Select(roll => TownsfolkRules.PickDestinationIndex(4, 2, max => roll))
            .ToList();

        reached.Should().BeEquivalentTo(new[] { 0, 1, 3 });
    }

    [Test]
    public void AWandererWithNoEntranceYet_PicksAnyOne()
    {
        TownsfolkRules.PickDestinationIndex(4, -1, Last).Should().Be(3);
        TownsfolkRules.PickDestinationIndex(4, 9, First).Should().Be(0, "a stale index from before a building went counts as none");
    }

    [Test]
    public void WithOneEntrance_ThatIsTheOnlyPlaceToGo()
    {
        TownsfolkRules.PickDestinationIndex(1, 0, Last).Should().Be(0);
        TownsfolkRules.PickDestinationIndex(0, -1, Last).Should().Be(-1);
    }

    [Test]
    public void APatrol_GoesAroundTheEntrancesByAngle()
    {
        // North, west, east, south of the centre, given out of order.
        var entrances = new[]
        {
            new Vector3(0, 10, 0),
            new Vector3(-10, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, -10, 0),
        };

        var order = TownsfolkRules.GetPatrolOrder(entrances);

        // Angles from -pi: west is +-pi (last), south is -pi/2, east is 0, north is pi/2.
        order.Should().Equal(3, 2, 0, 1);
    }

    [Test]
    public void APatrolOrder_IsStableForEntrancesAtTheSameAngle()
    {
        var entrances = new[] { new Vector3(5, 0, 0), new Vector3(10, 0, 0), new Vector3(-15, 0, 0) };

        TownsfolkRules.GetPatrolOrder(entrances).Should().BeEquivalentTo(new[] { 0, 1, 2 });
        TownsfolkRules.GetPatrolOrder(Array.Empty<Vector3>()).Should().BeEmpty();
    }

    [Test]
    public void AGuardPatrol_WrapsAround()
    {
        TownsfolkRules.GetNextPatrolIndex(-1, 4).Should().Be(0);
        TownsfolkRules.GetNextPatrolIndex(2, 4).Should().Be(3);
        TownsfolkRules.GetNextPatrolIndex(3, 4).Should().Be(0);
        TownsfolkRules.GetNextPatrolIndex(7, 4).Should().Be(0, "a stale stop wraps back into the route");
        TownsfolkRules.GetNextPatrolIndex(0, 0).Should().Be(-1);
    }

    [Test]
    public void Guards_StartSpreadAlongThePatrol()
    {
        var starts = Enumerable.Range(0, 4).Select(guard => TownsfolkRules.GetPatrolStartIndex(guard, 4, 8)).ToList();

        starts.Should().Equal(0, 2, 4, 6);
        TownsfolkRules.GetPatrolStartIndex(0, 2, 0).Should().Be(-1);
        TownsfolkRules.GetPatrolStartIndex(5, 2, 3).Should().BeInRange(0, 2);
        TownsfolkRules.GetPatrolStartIndex(1, 0, 3).Should().BeInRange(0, 2);
    }

    [Test]
    public void ACritter_AmblesPartWayToAnEntranceAndNeverOntoIt()
    {
        var from = new Vector3(0, 0, 0);
        var entrance = new Vector3(100, 0, 0);

        TownsfolkRules.PickCritterDestination(from, entrance, First).X.Should().BeApproximately(35f, 0.01f);
        TownsfolkRules.PickCritterDestination(from, entrance, Last).X.Should().BeApproximately(85f, 0.01f);
    }

    [TestCase(TownsfolkGroup.Wanderer, 4, 12)]
    [TestCase(TownsfolkGroup.Guard, 8, 16)]
    [TestCase(TownsfolkGroup.Critter, 6, 20)]
    public void EachGroup_PausesForItsOwnTimeOnArriving(TownsfolkGroup group, int shortest, int longest)
    {
        TownsfolkRules.GetPauseSeconds(group, First).Should().Be(shortest);
        TownsfolkRules.GetPauseSeconds(group, Last).Should().Be(longest);
    }

    [Test]
    public void ACreatureThatStopsMovingWhileWalking_IsStuckAfterThreeBeats()
    {
        var beats = 0;
        beats = TownsfolkRules.GetStuckBeats(true, 0.1f, beats);
        TownsfolkRules.IsStuck(beats).Should().BeFalse();
        beats = TownsfolkRules.GetStuckBeats(true, 0.0f, beats);
        TownsfolkRules.IsStuck(beats).Should().BeFalse();
        beats = TownsfolkRules.GetStuckBeats(true, 0.2f, beats);
        TownsfolkRules.IsStuck(beats).Should().BeTrue();
    }

    [Test]
    public void ProgressOrStandingStill_ResetsTheStuckCount()
    {
        TownsfolkRules.GetStuckBeats(true, 3.0f, 2).Should().Be(0, "it moved");
        TownsfolkRules.GetStuckBeats(false, 0.0f, 2).Should().Be(0, "it is pausing, not blocked");
    }

    #endregion

    #region An empty area costs nothing

    [Test]
    public void AnAreaEmptyForTwentyMinutes_LosesItsStreetLife()
    {
        TownsfolkRules.ShouldDespawnEmptyArea(Now.AddMinutes(-19), Now).Should().BeFalse();
        TownsfolkRules.ShouldDespawnEmptyArea(Now.AddMinutes(-20), Now).Should().BeTrue();
        TownsfolkRules.ShouldDespawnEmptyArea(Now.AddHours(-3), Now).Should().BeTrue();
    }

    [Test]
    public void AnAreaWithAPlayer_NeverLosesItsStreetLife()
    {
        TownsfolkRules.ShouldDespawnEmptyArea(null, Now).Should().BeFalse();
    }

    [Test]
    public void TheWait_IsTheSameAsTheSpawnServices()
    {
        TownsfolkRules.EmptyAreaDespawnMinutes.Should().Be(SWLOR.Game.Server.Service.Spawn.DespawnMinutes);
    }

    [Test]
    public void ACityIsReadAgainWhenTheRefreshIsDue()
    {
        TownsfolkRules.IsRefreshDue(Now.AddSeconds(1), Now).Should().BeFalse();
        TownsfolkRules.IsRefreshDue(Now, Now).Should().BeTrue();
        TownsfolkRules.IsRefreshDue(DateTime.MinValue, Now).Should().BeTrue();
    }

    #endregion

    #region Never hostile, never hurt

    [Test]
    public void StreetLife_IsNotInTheHostileFaction_SoItNeverCountsAsAHostileSpawn()
    {
        TownsfolkRules.NeutralFactionId.Should().NotBe(SpawnExclusionRules.HostileFactionId);
        SpawnExclusionRules.GetCreatureClass(TownsfolkRules.NeutralFactionId).Should().Be(SpawnExclusionClass.Other);
    }

    [Test]
    public void StreetLife_IsTheNeutralFactionOfTheModule()
    {
        // Module/fac/repute.fac.json lists PC, Hostile, Commoner, Merchant, Defender, Animals, Neutral in that order.
        TownsfolkRules.NeutralFactionId.Should().Be(6);
    }

    #endregion

    #region Who may use the switches

    private static readonly IReadOnlyDictionary<PropertyPermissionType, bool> NoPermissions =
        new Dictionary<PropertyPermissionType, bool>();

    private static readonly IReadOnlyDictionary<PropertyPermissionType, bool> EditTaxes =
        new Dictionary<PropertyPermissionType, bool> { [PropertyPermissionType.EditTaxes] = true };

    [Test]
    public void TheSwitches_UseTheChangeTaxesPower()
    {
        TownsfolkRules.SwitchPower.Should().Be(CityPower.ChangeTaxes);
    }

    [TestCase(CityRole.Mayor, true)]
    [TestCase(CityRole.ActingMayor, true)]
    [TestCase(CityRole.Officer, false)]
    [TestCase(CityRole.DepartedMayor, false)]
    [TestCase(CityRole.None, false)]
    public void ByRole_OnlyTheMayorAndAnActingMayorMayUseTheSwitches(CityRole role, bool expected)
    {
        TownsfolkRules.CanUseSwitches(role, NoPermissions).Should().Be(expected);
    }

    [Test]
    public void AHolderOfEditTaxes_MayUseTheSwitches()
    {
        TownsfolkRules.CanUseSwitches(CityRole.None, EditTaxes).Should().BeTrue();
        TownsfolkRules.CanUseSwitches(CityRole.Officer, EditTaxes).Should().BeTrue();
    }

    [Test]
    public void ADepartedMayor_MayNotUseTheSwitchesWhateverTheirRecordSays()
    {
        TownsfolkRules.CanUseSwitches(CityRole.DepartedMayor, EditTaxes).Should().BeFalse();
    }

    [Test]
    public void AnOrdinaryCitizen_MayNot()
    {
        TownsfolkRules.CanUseSwitches(CityRole.None, null).Should().BeFalse();
    }

    #endregion

    #region Which structures are walked to

    [Test]
    public void EveryCityBuildingAndHouse_IsWalkedTo()
    {
        var buildings = new[]
        {
            StructureType.CityHall, StructureType.StationCore, StructureType.Bank, StructureType.MedicalCenter,
            StructureType.Starport, StructureType.Cantina, StructureType.LabStyle1,
            StructureType.SmallHouseStyle1, StructureType.SmallHouseStyle2, StructureType.SmallHouseStyle3, StructureType.SmallHouseStyle4,
            StructureType.MediumHouseStyle1, StructureType.MediumHouseStyle2, StructureType.MediumHouseStyle3, StructureType.MediumHouseStyle4,
            StructureType.LargeHouseStyle1, StructureType.LargeHouseStyle2, StructureType.LargeHouseStyle3, StructureType.LargeHouseStyle4,
        };

        foreach (var building in buildings)
        {
            TownsfolkRules.IsEntranceStructure(building).Should().BeTrue($"{building} has an entrance door");
        }
    }

    [Test]
    public void StructuresWithNoDoor_AreNotWalkedTo()
    {
        TownsfolkRules.IsEntranceStructure(StructureType.DuelingRing).Should().BeFalse();
        TownsfolkRules.IsEntranceStructure(StructureType.GardenPlot).Should().BeFalse();
        TownsfolkRules.IsEntranceStructure(StructureType.Shipyard).Should().BeFalse();
        TownsfolkRules.IsEntranceStructure(StructureType.Invalid).Should().BeFalse();
    }

    [Test]
    public void EveryStructureWalkedTo_IsABuildingSpawnsKeepClearOfOrAStationCore()
    {
        var allowed = SpawnExclusionRules.QualifyingStructures.Append(StructureType.StationCore).ToHashSet();

        TownsfolkRules.EntranceStructures.Should().OnlyContain(structure => allowed.Contains(structure));
    }

    #endregion

    #region Text

    [Test]
    public void TheGazette_NamesEachSwitchChange()
    {
        TownsfolkRules.BuildGazetteText(TownsfolkSwitch.Townsfolk, true).Should().Contain("cleared").And.Contain("townsfolk");
        TownsfolkRules.BuildGazetteText(TownsfolkSwitch.Townsfolk, false).Should().Contain("returned");
        TownsfolkRules.BuildGazetteText(TownsfolkSwitch.Critters, true).Should().Contain("critters");
        TownsfolkRules.BuildGazetteText(TownsfolkSwitch.Critters, false).Should().Contain("returned");
        TownsfolkRules.BuildGazetteText(TownsfolkSwitch.Invalid, true).Should().BeEmpty();
    }

    [Test]
    public void TheButtons_ShowWhatEachGroupIsDoing()
    {
        TownsfolkRules.BuildButtonText(TownsfolkSwitch.Townsfolk, false).Should().Be("Townsfolk: Shown");
        TownsfolkRules.BuildButtonText(TownsfolkSwitch.Townsfolk, true).Should().Be("Townsfolk: Hidden");
        TownsfolkRules.BuildButtonText(TownsfolkSwitch.Critters, false).Should().Be("Critters: Shown");
        TownsfolkRules.BuildButtonText(TownsfolkSwitch.Critters, true).Should().Be("Critters: Hidden");
    }

    [Test]
    public void TheTooltips_SayStaffAndVendorsStayPut()
    {
        TownsfolkRules.BuildTooltip(TownsfolkSwitch.Townsfolk).Should().Contain("guards").And.Contain("Staff");
        TownsfolkRules.BuildTooltip(TownsfolkSwitch.Critters).Should().Contain("animals");
        TownsfolkRules.BuildTooltip(TownsfolkSwitch.Invalid).Should().BeEmpty();
    }

    [Test]
    public void TheResultLine_SaysWhatChanged()
    {
        TownsfolkRules.BuildResultText(TownsfolkSwitch.Townsfolk, true).Should().Be("Townsfolk are now hidden.");
        TownsfolkRules.BuildResultText(TownsfolkSwitch.Critters, false).Should().Be("Critters are now shown.");
    }

    [Test]
    public void EveryPlayerFacingString_IsPlainAscii()
    {
        var strings = new List<string>();
        foreach (var which in new[] { TownsfolkSwitch.Townsfolk, TownsfolkSwitch.Critters })
        {
            foreach (var hidden in new[] { true, false })
            {
                strings.Add(TownsfolkRules.BuildGazetteText(which, hidden));
                strings.Add(TownsfolkRules.BuildButtonText(which, hidden));
                strings.Add(TownsfolkRules.BuildResultText(which, hidden));
            }

            strings.Add(TownsfolkRules.BuildTooltip(which));
        }

        strings.Add(TownsfolkLooks.TownsfolkName);
        strings.Add(TownsfolkLooks.GuardName);
        strings.AddRange(TownsfolkLooks.Critters.Select(look => look.Name));

        strings.Should().OnlyContain(text => text.All(character => character >= ' ' && character <= '~'));
    }

    #endregion
}
