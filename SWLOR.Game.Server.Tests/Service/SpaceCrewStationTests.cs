using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Crew stations (plan section 10): which seats a hull has and which slots each works, the seat skills and rank
/// gate, station effectiveness for passives and actives, the seat registry and station fire, the crew permission
/// and the captain's Leadership bonus. Hulls follow the Starships (Revamp) Frigate, Cruiser and Command Ship rows.
/// </summary>
public class SpaceCrewStationTests
{
    private const uint Ship = 0x01000001;
    private const uint OtherShip = 0x01000002;
    private const uint Gunner = 0x01000011;
    private const uint Engineer = 0x01000012;

    private static ShipHullEntry Hull(ShipSize size, int high, int stationHigh, int mid, int stationMid, int low, int stationLow) => new()
    {
        Tag = "sd_test",
        Size = size,
        High = high,
        StationHigh = stationHigh,
        Mid = mid,
        StationMid = stationMid,
        Low = low,
        StationLow = stationLow,
        Hull = 100,
        Shield = 80,
        Capacitor = 60,
        CapacitorRegen = 2.5,
        Speed = 1.0,
        Defense = 8,
    };

    private static ShipHullEntry Frigate() => Hull(ShipSize.Large, 4, 2, 4, 2, 4, 2);
    private static ShipHullEntry Cruiser() => Hull(ShipSize.Capital, 6, 4, 5, 3, 5, 3);
    private static ShipHullEntry CommandShip() => Hull(ShipSize.Capital, 5, 3, 6, 4, 5, 3);

    private static ShipStatus Status(ShipHullEntry hull) => new()
    {
        Size = hull.Size,
        SlotTags = ShipStatCalculator.BuildSlotTags(hull),
    };

    [TearDown]
    public void ReleaseSeats()
    {
        foreach (var crewMember in new[] { Gunner, Engineer })
            SpaceCrew.ReleaseSeat(crewMember);
    }

    // Seats

    [Test]
    public void Seats_LargeHasAGunnerAndAnEngineer_CapitalAddsASecondGunnerAndSensors_SmallerHullsAreSolo()
    {
        ShipCrewSeats.GetSeats(ShipSize.Large).Should().Equal(ShipCrewSeat.Pilot, ShipCrewSeat.Gunner, ShipCrewSeat.Engineer);
        ShipCrewSeats.GetSeats(ShipSize.Capital).Should().Equal(
            ShipCrewSeat.Pilot, ShipCrewSeat.Gunner, ShipCrewSeat.SecondGunner, ShipCrewSeat.Engineer, ShipCrewSeat.Sensors);
        ShipCrewSeats.GetSeats(ShipSize.Small).Should().Equal(ShipCrewSeat.Pilot);
        ShipCrewSeats.GetSeats(ShipSize.Medium).Should().Equal(ShipCrewSeat.Pilot);

        ShipCrewSeats.GetStationSeats(ShipSize.Medium).Should().BeEmpty();
        ShipCrewSeats.GetStationSeats(ShipSize.Large).Should().Equal(ShipCrewSeat.Gunner, ShipCrewSeat.Engineer);
        ShipCrewSeats.HasSeat(ShipSize.Large, ShipCrewSeat.Sensors).Should().BeFalse();
    }

    [Test]
    public void Frigate_GunnerWorksTheHighStationSlots_EngineerTheMidAndLowOnes()
    {
        var status = Status(Frigate());

        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Pilot).Should().Equal(1, 2, 11, 12, 21, 22);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Gunner).Should().Equal(3, 4);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Engineer).Should().Equal(13, 14, 23, 24);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Sensors).Should().BeEmpty();
    }

    [Test]
    public void Cruiser_SplitsItsTurretsBetweenTwoGunners_AndSensorsWorksTheLastMidStationSlot()
    {
        var status = Status(Cruiser());

        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Gunner).Should().Equal(3, 4);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.SecondGunner).Should().Equal(5, 6);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Engineer).Should().Equal(13, 14, 15, 23, 24);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Sensors).Should().Equal(25);
    }

    [Test]
    public void CommandShip_GivesTheOddTurretToTheFirstGunner()
    {
        var status = Status(CommandShip());

        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Gunner).Should().Equal(3, 4);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.SecondGunner).Should().Equal(5);
        ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, ShipCrewSeat.Sensors).Should().Equal(26);
    }

    [Test]
    public void AnUntaggedSlot_BelongsToThePilot()
    {
        ShipCrewSeats.GetSeatForSlot(ShipSize.Large, null, 3).Should().Be(ShipCrewSeat.Pilot);
        ShipCrewSeats.GetSeatForSlot(ShipSize.Large, new Dictionary<int, ShipSlotTag>(), 3).Should().Be(ShipCrewSeat.Pilot);
    }

    // Seat skills and rank

    [Test]
    public void SeatSkills_GunnersCheckPiloting_TheEngineerEngineering_SensorsEspionage()
    {
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.Pilot).Should().Be(SkillType.Piloting);
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.Gunner).Should().Be(SkillType.Piloting);
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.SecondGunner).Should().Be(SkillType.Piloting);
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.Engineer).Should().Be(SkillType.Engineering);
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.Sensors).Should().Be(SkillType.Espionage);
    }

    [Test]
    public void RankGate_ComparesTheModulesRankWithTheSeatSkillRank()
    {
        var rank30 = new Dictionary<SkillType, int> { [SkillType.Piloting] = 30 };
        Func<SkillType, int> engineering(int rank) => skill => skill == SkillType.Engineering ? rank : 0;

        ShipCrewSeats.MeetsRank(null, SkillType.Engineering, engineering(0)).Should().BeTrue("a module with no requirement works for anyone");
        ShipCrewSeats.MeetsRank(new Dictionary<SkillType, int>(), SkillType.Engineering, engineering(0)).Should().BeTrue();
        ShipCrewSeats.MeetsRank(rank30, SkillType.Engineering, engineering(29)).Should().BeFalse();
        ShipCrewSeats.MeetsRank(rank30, SkillType.Engineering, engineering(30)).Should().BeTrue("an Engineer with Engineering 30 can work a rank-30 module");
        ShipCrewSeats.GetRequiredRank(new Dictionary<SkillType, int> { [SkillType.Piloting] = 20, [SkillType.Gathering] = 35 })
            .Should().Be(35);
    }

    [Test]
    public void RankGate_ChecksGatheringGearAgainstTheCrewMembersOwnGathering()
    {
        var miningLaser = new Dictionary<SkillType, int> { [SkillType.Gathering] = 30 };
        Func<SkillType, int> gunner(int piloting, int gathering) =>
            skill => skill switch { SkillType.Piloting => piloting, SkillType.Gathering => gathering, _ => 0 };

        ShipCrewSeats.MeetsRank(miningLaser, SkillType.Piloting, gunner(50, 0))
            .Should().BeFalse("Piloting does not stand in for Gathering at a Gunner seat");
        ShipCrewSeats.GetUnmetRequirement(miningLaser, SkillType.Piloting, gunner(50, 29))
            .Should().Be((SkillType.Gathering, 30));
        ShipCrewSeats.MeetsRank(miningLaser, SkillType.Piloting, gunner(0, 30)).Should().BeTrue();
    }

    [Test]
    public void TheCzerkaArmoredTransportsSignature_RaisesCrewTenPercentAndDroidCrewTwenty()
    {
        ShipCrewSeats.GetHullCrewBonusPercent(ShipCrewSeats.CzerkaArmoredTransportTag, false).Should().Be(10);
        ShipCrewSeats.GetHullCrewBonusPercent(ShipCrewSeats.CzerkaArmoredTransportTag, true).Should().Be(20);
        ShipCrewSeats.GetHullCrewBonusPercent("sd_terminuscorsa", true).Should().Be(0);
        ShipCrewSeats.GetHullCrewBonusPercent(null, false).Should().Be(0);
    }

    // Effectiveness

    [Test]
    public void SeatEffectiveness_IsTheCrewMultiplierOnlyWhileCrewedByOneWhoMeetsTheRank()
    {
        ShipCrewSeats.SeatEffectiveness(false, true, 1.35).Should().Be(0.0, "an empty seat works nothing, passives included");
        ShipCrewSeats.SeatEffectiveness(true, false, 1.35).Should().Be(0.0);
        ShipCrewSeats.SeatEffectiveness(true, true, 1.0).Should().Be(1.0, "a player at the module's rank works it at 100%");
        ShipCrewSeats.SeatEffectiveness(true, true, 1.35).Should().BeApproximately(1.05, 1e-9, "a seat works at 105% at most");
    }

    [Test]
    public void CrewEffectiveness_IsCappedAt105Percent_ForPlayersAndDroidsAlike()
    {
        ShipCrewSeats.MaximumSeatEffectiveness.Should().Be(1.05);
        ShipCrewSeats.CapEffectiveness(1.0).Should().Be(1.0);
        ShipCrewSeats.CapEffectiveness(1.05).Should().BeApproximately(1.05, 1e-9);
        ShipCrewSeats.CapEffectiveness(SpaceCrew.EffectivenessMultiplier(10, SpaceCommand.LeadershipCrewBonusPercent(50)))
            .Should().BeApproximately(1.05, 1e-9, "Crew Morale and Leadership 50 on a player crew stop at the cap");
        ShipCrewSeats.CapEffectiveness(-0.2).Should().Be(0.0);

        // A droid crew starts below the cap (W8b: 50-75%), so Crew Morale and Leadership lift it in full.
        ShipCrewSeats.SeatEffectiveness(true, true, 0.75 * 1.15).Should().BeApproximately(0.8625, 1e-9);
        ShipCrewSeats.SeatEffectiveness(true, true, 0.5 * 1.15).Should().BeApproximately(0.575, 1e-9);
    }

    [Test]
    public void ScaleByEffectiveness_RoundsAndKeepsAPositiveAmountAtLeastOne()
    {
        ShipCrewSeats.ScaleByEffectiveness(10, 1.0).Should().Be(10);
        ShipCrewSeats.ScaleByEffectiveness(10, 1.35).Should().Be(14);
        ShipCrewSeats.ScaleByEffectiveness(11, 1.1).Should().Be(12);
        ShipCrewSeats.ScaleByEffectiveness(1, 0.25).Should().Be(1);
        ShipCrewSeats.ScaleByEffectiveness(0, 1.35).Should().Be(0);
    }

    [Test]
    public void CrewMultiplier_AddsCrewMoraleAndLeadership()
    {
        SpaceCrew.EffectivenessMultiplier(10, SpaceCommand.LeadershipCrewBonusPercent(50)).Should().BeApproximately(1.15, 1e-9);
        SpaceCrew.EffectivenessMultiplier(0, SpaceCommand.LeadershipCrewBonusPercent(20)).Should().BeApproximately(1.02, 1e-9);
        SpaceCommand.LeadershipCrewBonusPercent(0).Should().Be(0);
        SpaceCommand.LeadershipCrewBonusPercent(25).Should().Be(3);
        SpaceCommand.LeadershipCrewBonusPercent(50).Should().Be(5, "a crewed Cruiser at 105% is level with five separate Fighters");
        SpaceCommand.LeadershipCrewBonusPercent(70).Should().Be(5);
    }

    [Test]
    public void StationPassives_CountOnlyWhileCrewed_AtTheCrewsEffectiveness()
    {
        var hull = Frigate();
        var modules = new[]
        {
            // Low slot 3 (hotbar 13) is an Engineer slot on the Frigate.
            new ShipFittedModule
            {
                Row = ShipModulePowerType.Low,
                Slot = 3,
                Step = new ShipModuleStep { Tag = "sm_plate", Row = ShipModulePowerType.Low, Hull = 40, ModuleDefense = 10 },
            },
        };

        var solo = ShipStatCalculator.Calculate(hull, modules, 0);
        solo.MaxHull.Should().Be(100);
        solo.Defense.Should().Be(8);

        var crewed = ShipStatCalculator.Calculate(hull, modules, 0, slot => slot == 13 ? 1.0 : 0.0);
        crewed.MaxHull.Should().Be(100 + 44, "40 hull x 1.1 on a Large hull");
        crewed.Defense.Should().Be(18);

        var inspired = ShipStatCalculator.Calculate(hull, modules, 0, _ => 1.25);
        inspired.MaxHull.Should().Be(100 + 55, "40 x 1.1 x 1.25");
        inspired.Defense.Should().Be(18, "the best-of Defense applies in full while crewed, as the balance model does");
    }

    // Registry and station fire

    [Test]
    public void Seats_HoldOneCrewMemberEach_AndACrewMemberHoldsOneSeat()
    {
        var status = Status(Frigate());

        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Gunner, Gunner).Should().BeTrue();
        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Gunner, Engineer).Should().BeFalse("the seat is taken");
        SpaceCrew.AssignSeat(OtherShip, ShipCrewSeat.Engineer, Gunner).Should().BeFalse("a crew member holds one seat");
        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Pilot, Engineer).Should().BeFalse("the pilot's seat is the pilot's");
        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Engineer, Engineer).Should().BeTrue();

        SpaceCrew.IsSeatCrewed(Ship, status, 3).Should().BeTrue();
        SpaceCrew.IsSeatCrewed(Ship, status, 13).Should().BeTrue();
        SpaceCrew.IsSeatCrewed(Ship, status, 1).Should().BeFalse("Helm slots are never crewed");
        SpaceCrew.IsSeatCrewed(OtherShip, status, 3).Should().BeFalse();
        SpaceCrew.GetCrew(Ship).Should().BeEquivalentTo(new[] { Gunner, Engineer });

        SpaceCrew.TryGetSeat(Gunner, out var ship, out var seat).Should().BeTrue();
        ship.Should().Be(Ship);
        seat.Should().Be(ShipCrewSeat.Gunner);

        SpaceCrew.ReleaseSeat(Gunner).Should().BeTrue();
        SpaceCrew.ReleaseSeat(Gunner).Should().BeFalse();
        SpaceCrew.IsSeatCrewed(Ship, status, 3).Should().BeFalse();
        SpaceCrew.GetSeatOccupant(Ship, ShipCrewSeat.Engineer).Should().Be(Engineer);
    }

    [Test]
    public void StationFire_ReadsTheCrewMembersStats_AndFiresOnlyTheirSeatsSlots()
    {
        var status = Status(Frigate());

        SpaceCrew.GetStatSource(Ship).Should().Be(Ship);
        SpaceCrew.GetFireEffectiveness(Ship).Should().Be(1.0);
        SpaceCrew.GetCreditedSkill(Ship, SkillType.Piloting).Should().Be(SkillType.Piloting);
        SpaceCrew.CanActingSeatFire(Ship, status, 1).Should().BeTrue();
        SpaceCrew.CanActingSeatFire(Ship, status, 3).Should().BeFalse("the pilot cannot fire station slots");

        using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext
               {
                   Ship = Ship, CrewMember = Engineer, Seat = ShipCrewSeat.Engineer, Effectiveness = 1.1, Skill = SkillType.Engineering,
               }))
        {
            SpaceCrew.GetStatSource(Ship).Should().Be(Engineer);
            SpaceCrew.GetCreditedPlayer(Ship).Should().Be(Engineer);
            SpaceCrew.GetFireEffectiveness(Ship).Should().BeApproximately(1.1, 1e-9);
            SpaceCrew.GetCreditedSkill(Ship, SkillType.Piloting).Should().Be(SkillType.Engineering, "crew earn XP in their seat's skill");
            SpaceCrew.CanActingSeatFire(Ship, status, 13).Should().BeTrue();
            SpaceCrew.CanActingSeatFire(Ship, status, 3).Should().BeFalse("an Engineer cannot fire the Gunner's turrets");
            SpaceCrew.GetStatSource(OtherShip).Should().Be(OtherShip, "another ship fires for its own pilot");

            using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext { Ship = Ship, CrewMember = Gunner, Seat = ShipCrewSeat.Gunner }))
            {
                SpaceCrew.GetStatSource(Ship).Should().Be(Gunner);
            }

            SpaceCrew.GetStatSource(Ship).Should().Be(Engineer, "the outer station fire resumes");
        }

        SpaceCrew.GetStatSource(Ship).Should().Be(Ship);
        using (SpaceCrew.BeginStationFire(null))
        {
            SpaceCrew.GetStatSource(Ship).Should().Be(Ship, "the pilot's own fire passes no context");
        }
    }

    // Leadership and permission

    [Test]
    public void LeadershipBonus_IsHalfAPercentPerRank_UpToTwentyFivePercentAtFifty()
    {
        SpaceCommand.LeadershipBonusPercent(0).Should().Be(0);
        SpaceCommand.LeadershipBonusPercent(1).Should().Be(1);
        SpaceCommand.LeadershipBonusPercent(25).Should().Be(13);
        SpaceCommand.LeadershipBonusPercent(40).Should().Be(20);
        SpaceCommand.LeadershipBonusPercent(50).Should().Be(25);
        SpaceCommand.LeadershipBonusPercent(60).Should().Be(25);
        SpaceCommand.LeadershipBonusPercent(-3).Should().Be(0);

        // Focus Fire III at SOC 26 (+12%) under a Leadership 50 captain.
        SpaceCombatMath.ScaleByPercent(12, SpaceCommand.LeadershipBonusPercent(50)).Should().Be(15);
        SpaceCommand.ApplyLeadershipScaling(0x7F000000u, 12).Should().Be(12, "only a ship in space scales its orders by rank");
    }

    [Test]
    public void CrewPermission_IsAStarshipPermissionNextToPilotShip()
    {
        ((int)PropertyPermissionType.CrewStation).Should().Be(17, "permission records are keyed by the saved number");
        typeof(PropertyPermissionType).GetField(nameof(PropertyPermissionType.CrewStation))!
            .GetCustomAttributes(typeof(PropertyPermissionAttribute), false)
            .Cast<PropertyPermissionAttribute>()
            .Single().IsActive.Should().BeTrue();

        var property = ReadServerSource("Service", "Property.cs");
        property.Should().Contain("PropertyPermissionType.PilotShip,\n                PropertyPermissionType.CrewStation,");

        var stations = ReadServerSource("Service", "SpaceCrewStations.cs");
        stations.Should().Contain("PropertyPermissionType.CrewStation, out var allowed");
        stations.Should().Contain("Party.IsInParty(pilot, player)");
    }

    [Test]
    public void TheSpaceCode_RoutesCrewWorkThroughTheStations()
    {
        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("var source = SpaceCrew.GetStatSource(activator);");
        space.Should().Contain("ActivateShipModuleCore(ship, feat, false, activator, seat);");
        space.Should().Contain("ReleaseShipCrew(player, ");
        space.Should().Contain("RestoreCrewMemberAfterLogin(player);");
        space.Should().Contain("CrewMemberDisconnected(player);");

        // A destroyed ship ejects everyone at its stations and viewports in escape pods (W2d).
        var death = ReadServerSource("Service", "SpaceDeath.cs");
        death.Should().Contain("LeaveCrewStation(crewMember, returnToConsole: false);");
        death.Should().Contain("EscapePodRules.BuildManifest(creature, stationCrew");

        var toggles = ReadServerSource("Service", "SpaceToggles.cs");
        toggles.Should().Contain("? BuildDroidFireContext(creature, droid)");
        toggles.Should().Contain(": BuildCrewFireContext(creature, crewMember, seat)))");

        var actions = ReadServerSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        actions.Should().Contain("SpaceCrew.GetStatSource(repairer)");

        ReadServerSource("Feature", "UsePerkFeat.cs").Should().Contain(
            "if (Space.IsAtCrewStation(activator))\n                return TryUseCrewStationAbility(activator, target, feat, targetLocation, ability);");
        ReadServerSource("Feature", "StatusEffectDefinition", "SocialScalingStatusEffectBase.cs")
            .Should().Contain("SpaceCommand.ApplyLeadershipScaling(");
        var breakFormation = ReadServerSource("Feature", "AbilityDefinition", "Piloting", "BreakFormationAbilityDefinition.cs");
        breakFormation.Should().Contain("new FlashStatusEffect(SpaceCommand.ApplyLeadershipScaling(activator,");
        breakFormation.Should().Contain("new WeakenedStatusEffect(SpaceCommand.ApplyLeadershipScaling(activator,");
        var crew = ReadServerSource("Service", "SpaceService", "SpaceCrew.cs");
        crew.Should().Contain("ShipCrewSeats.CapEffectiveness(GetCrewMultiplier(ship))");
        crew.Should().Contain("DroidCrewRules.SeatEffectiveness(droid.Seat, droid.Industry, GetCrewMultiplier(ship, true)");
    }

    // Crew members' own Piloting abilities (W11b)

    [Test]
    public void StationAbilities_OnlyPilotingAbilities_AndNeverFromAViewport()
    {
        SpaceCrewAbilityRules.GetStationAbilityError(true, true, SkillType.Piloting).Should().BeEmpty();
        SpaceCrewAbilityRules.GetStationAbilityError(false, true, SkillType.Piloting).Should().Be(SpaceCrewAbilityRules.ViewportError);
        SpaceCrewAbilityRules.GetStationAbilityError(true, false, SkillType.Piloting)
            .Should().Be(SpaceCrewAbilityRules.NotPilotingError, "a ground-only ability has no ship to act through");
        SpaceCrewAbilityRules.GetStationAbilityError(true, true, SkillType.Leadership).Should().Be(SpaceCrewAbilityRules.NotPilotingError);
        SpaceCrewAbilityRules.GetStationAbilityError(true, true, SkillType.Espionage).Should().Be(SpaceCrewAbilityRules.NotPilotingError);
    }

    [Test]
    public void StationAbility_TheShipActs_WhileTheCrewMemberIsItsUserAndStatSource()
    {
        var status = Status(Frigate());
        SpaceCrew.GetAbilityUser(Ship).Should().Be(Ship, "the pilot uses their own abilities");

        // A crew member's module fire reads their stats but is not an ability: recasts and STM stay the ship's.
        using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext { Ship = Ship, CrewMember = Gunner, Seat = ShipCrewSeat.Gunner }))
        {
            SpaceCrew.GetStatSource(Ship).Should().Be(Gunner);
            SpaceCrew.GetAbilityUser(Ship).Should().Be(Ship);
        }

        using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext
               {
                   Ship = Ship, CrewMember = Gunner, Seat = ShipCrewSeat.Gunner, Skill = SkillType.Piloting, IsAbility = true,
               }))
        {
            SpaceCrew.GetAbilityUser(Ship).Should().Be(Gunner, "the crew member pays the STM and takes the recast");
            SpaceCrew.GetStatSource(Ship).Should().Be(Gunner, "their attributes and perks set the ability's strength");
            SpaceCrew.GetCreditedPlayer(Ship).Should().Be(Gunner);
            SpaceCrew.GetCreditedSkill(Ship, SkillType.Engineering).Should().Be(SkillType.Piloting, "a Piloting ability earns Piloting");
            SpaceCrew.CanActingSeatFire(Ship, status, 3).Should().BeTrue("their abilities fire their own seat's weapons");
            SpaceCrew.CanActingSeatFire(Ship, status, 1).Should().BeFalse("never the pilot's Helm weapons");
            SpaceCrew.GetAbilityUser(OtherShip).Should().Be(OtherShip);
            SpaceCrew.GetAbilityUser(Gunner).Should().Be(Gunner);
        }

        SpaceCrew.GetAbilityUser(Ship).Should().Be(Ship);
        SpaceCrew.GetStatSource(Ship).Should().Be(Ship);
    }

    [Test]
    public void StationAbility_IsWiredThroughTheShip()
    {
        var ability = ReadServerSource("Service", "Ability.cs");
        ability.Should().NotContain("You cannot use abilities while at a crew station.", "the station refusal moved to the crew path");
        ability.Should().Contain("var user = SpaceCrew.GetAbilityUser(activator);");
        ability.Should().Contain("SendMessageToPC(user, reason);");
        ability.Should().Contain("req.CheckRequirements(user, ability)");
        ability.Should().Contain("Recast.IsOnRecastDelay(user, ability.RecastGroup)");

        var feat = ReadServerSource("Feature", "UsePerkFeat.cs");
        feat.Should().Contain("var context = Space.GetCrewStationAbilityContext(crewMember, ability, out var error);");
        feat.Should().Contain("Perk.GetPerkLevel(crewMember, ability.EffectiveLevelPerkType)");
        feat.Should().Contain("using var crewAbility = SpaceCrew.BeginStationFire(context);");
        feat.Should().Contain("Ability.CanUseAbility(ship, target, feat, effectivePerkLevel, targetLocation)");
        feat.Should().Contain("HoldForHostileAbilityDelay(crewMember, target, feat, targetLocation, ability)");
        feat.Should().Contain("ExecuteAbilityImpact(ship, target, feat, ability, targetLocation);");
        feat.Should().Contain("Recast.ApplyRecastDelay(crewMember, ability.RecastGroup, recastDelay);");
        feat.Should().Contain("req.AfterActivationAction(payer, ability);");
        feat.Should().Contain("SpaceAbilities.GetSelectedTarget(ship, crewMember)");

        var stations = ReadServerSource("Service", "SpaceCrewStations.cs");
        stations.Should().Contain("IsAbility = true,");

        // Command orders: the crew member's Command Presence, Social and Leadership; Mark Target their own perk and cooldown.
        var command = ReadServerSource("Service", "SpaceCommand.cs");
        command.Should().Contain("LeadershipBonusPercent(GetCaptainLeadershipRank(SpaceCrew.GetAbilityUser(ship)))");
        command.Should().Contain("Stat.GetStatAdjustment(SpaceCrew.GetAbilityUser(ship), StatType.SpaceCommandRangeBonusMeters)");
        command.Should().Contain("var user = SpaceCrew.GetAbilityUser(commander);");
        command.Should().Contain("_markTargetReadyAt[user] = now.AddSeconds(cooldown);");
        command.Should().Contain("CombatPoint.AddSupportCombatPoint(SpaceCrew.GetAbilityUser(commander), affected");
        ReadServerSource("Feature", "AbilityDefinition", "AbilityEffectScaling.cs").Should().Contain("source = SpaceCrew.GetAbilityUser(source);");
        ReadServerSource("Feature", "AbilityDefinition", "Piloting", "EmergencyRepairAbilityDefinition.cs")
            .Should().Contain("GetAbilityScore(SpaceCrew.GetAbilityUser(activator), AbilityType.Willpower)");
        ReadServerSource("Feature", "AbilityDefinition", "Piloting", "DoubleShotAbilityDefinition.cs")
            .Should().Contain("Combat.RestoreAbilityHitStamina(\n                        user,");
    }

    [Test]
    public void ViewportPassengers_LeaveTheirGroundEffectsBehind()
    {
        var source = ReadServerSource("Service", "SpaceCrewStations.cs");
        var start = source.IndexOf("public static bool BoardCrewStation(", StringComparison.Ordinal);
        var board = source.Substring(start, source.IndexOf("private static uint CreateCrewCopy(", start, StringComparison.Ordinal) - start);
        var cleanup = board.IndexOf("StatusEffect.RemoveAllStatusEffects(player);", StringComparison.Ordinal);
        var hotBar = board.IndexOf("dbPlayer.SerializedHotBar", StringComparison.Ordinal);
        cleanup.Should().BeGreaterThan(hotBar);
        board.Substring(hotBar, cleanup - hotBar).Should().NotContain("isViewer",
            "a passenger's ground aura must not keep buffing the crew in flight");
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
