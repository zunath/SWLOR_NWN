using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Droid crew (plan sections 10 and 27): effectiveness by Industry and seat from the W0 balance numbers, the crew cap,
/// the tier's module rank, the tier's attributes, what a droid does with each module, the follow-limit exclusion, the
/// posting rules, the seat registry (a seat holds a player or a droid) and a droid's station fire.
/// </summary>
public class SpaceDroidCrewTests
{
    private const uint Ship = 0x01000101;
    private const uint Player = 0x01000111;
    private const uint Invalid = 0x7F000000;

    private static ShipStatus Frigate() => new()
    {
        Size = ShipSize.Large,
        SlotTags = ShipStatCalculator.BuildSlotTags(new ShipHullEntry
        {
            Tag = "sd_test", Size = ShipSize.Large, High = 4, StationHigh = 2, Mid = 4, StationMid = 2, Low = 4, StationLow = 2,
            Hull = 100, Shield = 80, Capacitor = 60, CapacitorRegen = 2.5, Speed = 1.0, Defense = 8,
        }),
    };

    private static ShipDroidCrew Droid(ShipCrewSeat seat, int tier = 5, int industry = 100) => new()
    {
        Seat = seat, OwnerPlayerId = "owner", DroidItemId = $"droid-{seat}-{tier}", Name = "R5", Tier = tier, Industry = industry,
    };

    [TearDown]
    public void ClearSeats()
    {
        SpaceCrew.ClearDroidCrew(Ship);
        SpaceCrew.ReleaseSeat(Player);
    }

    // Effectiveness

    [TestCase(0, 0.25)]
    [TestCase(20, 0.30)]
    [TestCase(40, 0.35)]
    [TestCase(60, 0.40)]
    [TestCase(80, 0.45)]
    [TestCase(100, 0.50)]
    public void GunnerSeats_RiseWithIndustryFromAQuarterToHalfForTheBestIndustrialDroid(int industry, double expected)
    {
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Gunner, industry).Should().BeApproximately(expected, 1e-9);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.SecondGunner, industry).Should().BeApproximately(expected, 1e-9);
    }

    [TestCase(0, 0.25)]
    [TestCase(20, 0.35)]
    [TestCase(40, 0.45)]
    [TestCase(60, 0.55)]
    [TestCase(80, 0.65)]
    [TestCase(100, 0.75)]
    public void EngineerAndSensorsSeats_RiseWithIndustryFromAQuarterToThreeQuartersForTheBestIndustrialDroid(int industry, double expected)
    {
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, industry).Should().BeApproximately(expected, 1e-9);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Sensors, industry).Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void Droids_NeverFly_TheBayIsNoStation_AndIndustryOutsideZeroToOneHundredIsClamped()
    {
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Pilot, 100).Should().Be(0);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Invalid, 100).Should().Be(0);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.DroidBay, 100).Should().Be(0, "a bay droid's Industry pays off through the bay");
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, -5).Should().BeApproximately(0.25, 1e-9);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, 999).Should().BeApproximately(0.75, 1e-9);
    }

    [Test]
    public void CrewMoraleAndLeadership_MultiplyADroidsBase_UpToTheSeatCap()
    {
        // Crew Morale +10% and Leadership 50 (+5%).
        var multiplier = SpaceCrew.EffectivenessMultiplier(10, 5);
        DroidCrewRules.SeatEffectiveness(ShipCrewSeat.Engineer, 100, multiplier).Should().BeApproximately(0.8625, 1e-9);
        DroidCrewRules.SeatEffectiveness(ShipCrewSeat.Gunner, 100, multiplier).Should().BeApproximately(0.575, 1e-9);
        DroidCrewRules.SeatEffectiveness(ShipCrewSeat.Gunner, 0, 1.0).Should().BeApproximately(0.25, 1e-9);

        DroidCrewRules.SeatEffectiveness(ShipCrewSeat.Engineer, 100, 3.0)
            .Should().Be(ShipCrewSeats.MaximumSeatEffectiveness, "every seat is capped at 105%, droid seats included");
        DroidCrewRules.SeatEffectiveness(ShipCrewSeat.Engineer, 100, -1.0).Should().Be(0);
    }

    [Test]
    public void TheBestIndustrialDroid_WorksAtThreeQuartersOfAPlayer_AndStaysUnderTheCrewCap()
    {
        // A player at the module's rank works a seat at 100% (ShipCrewSeats.SeatEffectiveness).
        var player = ShipCrewSeats.SeatEffectiveness(true, true, 1.0);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, DroidIndustryTuning.MaximumIndustry)
            .Should().BeApproximately(0.75 * player, 1e-9);

        // Crew Morale, Leadership 50 and a further +20% (the Czerka Armored Transport's drafted droid crew bonus).
        var multiplier = SpaceCrew.EffectivenessMultiplier(10, 5) + 0.20;
        foreach (var seat in new[] { ShipCrewSeat.Gunner, ShipCrewSeat.SecondGunner, ShipCrewSeat.Engineer, ShipCrewSeat.Sensors })
        {
            (DroidCrewRules.BaseEffectiveness(seat, DroidIndustryTuning.MaximumIndustry) * multiplier)
                .Should().BeLessThanOrEqualTo(ShipCrewSeats.MaximumSeatEffectiveness, seat.ToString());
            DroidCrewRules.SeatEffectiveness(seat, DroidIndustryTuning.MaximumIndustry, 100.0)
                .Should().Be(ShipCrewSeats.MaximumSeatEffectiveness, "the cap binds whatever the multiplier");
        }
    }

    [Test]
    public void IndustryBonus_IsWhatIndustryAddsOverACombatDroid()
    {
        SpaceCrew.GetDroidIndustryBonus(Droid(ShipCrewSeat.Engineer)).Should().BeApproximately(0.50, 1e-9);
        SpaceCrew.GetDroidIndustryBonus(Droid(ShipCrewSeat.Gunner)).Should().BeApproximately(0.25, 1e-9);
        SpaceCrew.GetDroidIndustryBonus(Droid(ShipCrewSeat.Engineer, 5, 0)).Should().Be(0, "a combat droid has no Industry");
        SpaceCrew.GetDroidIndustryBonus(Droid(ShipCrewSeat.Engineer, 3, 50)).Should().BeApproximately(0.25, 1e-9);
        SpaceCrew.GetDroidIndustryBonus(Droid(ShipCrewSeat.DroidBay)).Should().Be(0);
        SpaceCrew.GetDroidIndustryBonus(null).Should().Be(0);
        DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, 0).Should().BeApproximately(DroidCrewRules.NoIndustryEffectiveness, 1e-9,
            "a tier-5 combat droid makes poor crew: its tier sets only the module ranks");
    }

    // Tier gates and attributes

    [TestCase(1, 5)]
    [TestCase(2, 15)]
    [TestCase(3, 25)]
    [TestCase(4, 35)]
    [TestCase(5, 45)]
    public void ATiersModuleRank_IsItsDroidLevel(int tier, int rank)
    {
        DroidCrewRules.Level(tier).Should().Be(rank);
        DroidCrewRules.MaximumModuleRank(tier).Should().Be(rank);
        DroidCrewRules.MeetsRank(new Dictionary<SkillType, int> { [SkillType.Piloting] = rank }, tier).Should().BeTrue();
        DroidCrewRules.MeetsRank(new Dictionary<SkillType, int> { [SkillType.Engineering] = rank + 5 }, tier).Should().BeFalse();
    }

    [Test]
    public void ModulesWithNoRank_WorkForEveryTier_AndNamedRankFiftyModulesForNone()
    {
        DroidCrewRules.MeetsRank(null, 1).Should().BeTrue();
        DroidCrewRules.MeetsRank(new Dictionary<SkillType, int>(), 1).Should().BeTrue();
        DroidCrewRules.MeetsRank(new Dictionary<SkillType, int> { [SkillType.Piloting] = 50 }, 5).Should().BeFalse();
    }

    [TestCase(1, 17)]
    [TestCase(2, 19)]
    [TestCase(3, 21)]
    [TestCase(4, 23)]
    [TestCase(5, 25)]
    public void ADroidsAttributes_AreTheBalanceModelsPilotAtItsLevel(int tier, int attribute)
    {
        DroidCrewRules.Attribute(tier).Should().Be(attribute);
    }

    // Behavior

    [Test]
    public void Droids_RunToggledWeaponsAndShieldAndHullSupport_AndLeaveEverythingElse()
    {
        DroidCrewRules.GetTask(ShipModuleType.CombatLaser, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.Weapon);
        DroidCrewRules.GetTask(ShipModuleType.Missile, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.Weapon);
        DroidCrewRules.GetTask(ShipModuleType.ShieldRepairer, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ShieldSupport);
        DroidCrewRules.GetTask(ShipModuleType.ShieldHardener, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ShieldSupport);
        DroidCrewRules.GetTask(ShipModuleType.BulwarkShieldGenerator, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ShieldSupport);
        DroidCrewRules.GetTask(ShipModuleType.HullRepairer, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.HullSupport);
        DroidCrewRules.GetTask(ShipModuleType.RepairFieldGenerator, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ShieldSupport);

        DroidCrewRules.GetTask(ShipModuleType.MiningLaser, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.None);
        DroidCrewRules.GetTask(ShipModuleType.HypermatterInjector, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.None);
        DroidCrewRules.GetTask(ShipModuleType.CombatLaser, ShipModuleActivation.OnePress).Should().Be(DroidCrewTask.None);
        DroidCrewRules.GetTask(ShipModuleType.ProbeLauncher, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.Survey);
        DroidCrewRules.GetTask(ShipModuleType.CapitalEwar, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ElectronicWarfare);

        DroidCrewRules.RunsWhenShieldLow(ShipModuleType.ShieldHardener).Should().BeTrue();
        DroidCrewRules.RunsWhenHullLow(ShipModuleType.ShieldHardener).Should().BeFalse();
        DroidCrewRules.RepairThreshold.Should().Be(0.25f, "droids repair at the threshold NPC ships use");
    }

    [Test]
    public void ElectronicWarfare_RunsWhileAnEnemyIsInRange_AndStopsOnceNoneIs()
    {
        DroidCrewRules.FlipsElectronicWarfare(isOn: false, enemyInRange: true).Should().BeTrue("an enemy came in range");
        DroidCrewRules.FlipsElectronicWarfare(isOn: true, enemyInRange: false).Should().BeTrue("nothing left to jam");
        DroidCrewRules.FlipsElectronicWarfare(isOn: true, enemyInRange: true).Should().BeFalse();
        DroidCrewRules.FlipsElectronicWarfare(isOn: false, enemyInRange: false).Should().BeFalse();
    }

    [Test]
    public void ElectronicWarfare_IsWiredIntoTheDroidStations_AtStationEffectiveness()
    {
        var crew = ReadServerSource("Service", "SpaceDroidCrew.cs");
        crew.Should().Contain("UpdateDroidElectronicWarfare(pilot, status, slots, posted);")
            .And.Contain("DroidCrewRules.FlipsElectronicWarfare(isOn, HasEnemyShipWithin(pilot, step.Range))")
            .And.Contain("AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: droid));");
        crew.IndexOf("UpdateDroidElectronicWarfare(pilot, status, slots, posted);", System.StringComparison.Ordinal)
            .Should().BeLessThan(crew.IndexOf("if (!hostile)", System.StringComparison.Ordinal),
                "the array also switches off when the pilot has no hostile target");

        ReadServerSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs")
            .Should().Contain("ShipCrewSeats.ScaleByEffectiveness(\n                        Space.ApplySeatSkillBonus(activator, ShipModuleType.CapitalEwar, Effective(step).AuraAccuracy),\n                        SpaceCrew.GetFireEffectiveness(activator));",
                "the runner's Espionage rank (W11n) comes first, then the station's effectiveness");
    }

    [Test]
    public void AWeapon_EngagesEachPickOfTargetOnce()
    {
        const uint Enemy = 0x01000201;
        const uint OtherEnemy = 0x01000202;

        DroidCrewRules.MayEngageAgain(Enemy, Invalid).Should().BeTrue();
        DroidCrewRules.MayEngageAgain(Enemy, Enemy).Should().BeFalse("after an auto-off it waits for a new pick");
        DroidCrewRules.MayEngageAgain(OtherEnemy, Enemy).Should().BeTrue();

        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Gunner)).Should().BeTrue();
        SpaceCrew.GetDroidEngagement(Ship, 3).Should().Be(Invalid);
        SpaceCrew.RecordDroidEngagement(Ship, 3, Enemy);
        SpaceCrew.GetDroidEngagement(Ship, 3).Should().Be(Enemy);
        SpaceCrew.ForgetDroidEngagements(Ship);
        SpaceCrew.GetDroidEngagement(Ship, 3).Should().Be(Invalid, "the pilot picking a target, the same one included, lets droids engage again");
    }

    // Follow limit and posting

    [Test]
    public void PostedDroids_DoNotCountTowardTheFollowLimit_AndCannotBeActivated()
    {
        DroidCrewRules.CountsTowardFollowLimit(spawned: true, posted: false).Should().BeTrue();
        DroidCrewRules.CountsTowardFollowLimit(spawned: false, posted: true).Should().BeFalse();
        DroidCrewRules.CountsTowardFollowLimit(spawned: false, posted: false).Should().BeFalse();

        DroidCrewRules.GetActivationError(thisDroidPosted: false, followers: 0).Should().BeEmpty(
            "droids posted at stations leave the follow slot free");
        DroidCrewRules.GetActivationError(thisDroidPosted: false, followers: 1).Should().Be("Only one droid may be activated at a time.");
        DroidCrewRules.GetActivationError(thisDroidPosted: true, followers: 0).Should().Contain("posted");
    }

    [Test]
    public void Posting_NeedsTheCaptainTheirOwnFreeDroidAndAnEmptyStation()
    {
        string Error(bool captain = true, bool seatExists = true, bool seatHasPlayer = false, bool seatHasDroid = false,
            bool owns = true, bool posted = false, bool following = false) =>
            DroidCrewRules.GetPostingError(captain, seatExists, seatHasPlayer, seatHasDroid, owns, posted, following,
                ShipCrewSeats.GetPostName(ShipCrewSeat.Gunner));

        Error().Should().BeEmpty();
        Error(captain: false).Should().Contain("permission");
        Error(seatExists: false).Should().Contain("no such station");
        Error(owns: false).Should().Contain("controller you carry");
        Error(posted: true).Should().Contain("already posted");
        Error(following: true).Should().Contain("Dismiss");
        Error(seatHasPlayer: true).Should().Be("Someone works the Gunner station.");
        DroidCrewRules.GetPostingError(true, true, false, true, true, false, false, ShipCrewSeats.GetPostName(ShipCrewSeat.DroidBay))
            .Should().Be("A droid already works the droid bay. Recall it first.");
        Error(seatHasDroid: true).Should().Contain("Recall it first");
        DroidCrewRules.PlayerBumpsDroid.Should().BeTrue("a player taking a droid's station sends the droid back to its owner");
    }

    // Seats

    [Test]
    public void ASeat_HoldsAPlayerOrADroid_NotBoth()
    {
        var status = Frigate();
        const int GunnerSlot = 3;

        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Gunner)).Should().BeTrue();
        SpaceCrew.IsSeatCrewed(Ship, status, GunnerSlot).Should().BeTrue("a droid crews its station");
        SpaceCrew.IsSeatWorked(Ship, ShipCrewSeat.Gunner).Should().BeTrue();
        SpaceCrew.GetSeatOccupant(Ship, ShipCrewSeat.Gunner).Should().Be(Invalid, "the droid is not a player occupant");
        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Gunner, Player).Should().BeFalse("the droid must be bumped first");
        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Gunner, 3)).Should().BeFalse();

        SpaceCrew.ReleaseDroidSeat(Ship, ShipCrewSeat.Gunner).Should().BeTrue();
        SpaceCrew.ReleaseDroidSeat(Ship, ShipCrewSeat.Gunner).Should().BeFalse();
        SpaceCrew.AssignSeat(Ship, ShipCrewSeat.Gunner, Player).Should().BeTrue();
        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Gunner)).Should().BeFalse("a player holds the seat");

        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Pilot)).Should().BeFalse("droids never fly");
        SpaceCrew.AssignDroidSeat(Ship, Droid(ShipCrewSeat.Engineer)).Should().BeTrue();
        SpaceCrew.GetDroidCrew(Ship).Should().ContainSingle(x => x.Seat == ShipCrewSeat.Engineer);
        SpaceCrew.GetShipsWithDroidCrew().Should().Contain(Ship);

        SpaceCrew.ClearDroidCrew(Ship);
        SpaceCrew.GetSeatDroid(Ship, ShipCrewSeat.Engineer).Should().BeNull();
        SpaceCrew.GetShipsWithDroidCrew().Should().NotContain(Ship);
    }

    [Test]
    public void DroidFire_ReadsTheDroidsTierNotACreature_AndCreditsThePilot()
    {
        var droid = Droid(ShipCrewSeat.Gunner, 3);
        SpaceCrew.TryGetDroidAttribute(Invalid, AbilityType.Perception, out _).Should().BeFalse();
        SpaceCrew.IsDroidStatSource(Invalid).Should().BeFalse();

        using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext
               {
                   Ship = Ship, CrewMember = Invalid, Droid = droid, Seat = droid.Seat, Effectiveness = 0.375, Skill = SkillType.Piloting,
               }))
        {
            var source = SpaceCrew.GetStatSource(Ship);
            source.Should().Be(Invalid, "a droid has no creature, so no perks or statuses apply");
            SpaceCrew.IsDroidStatSource(source).Should().BeTrue();
            SpaceCrew.TryGetDroidAttribute(source, AbilityType.Perception, out var per).Should().BeTrue();
            per.Should().Be(21);
            SpaceCrew.TryGetDroidAttribute(source, AbilityType.Might, out var mgt).Should().BeTrue();
            mgt.Should().Be(21);
            SpaceCrew.TryGetDroidAttribute(Ship, AbilityType.Perception, out _).Should().BeFalse("the pilot keeps their own scores");
            SpaceCrew.GetFireEffectiveness(Ship).Should().BeApproximately(0.375, 1e-9);
            SpaceCrew.GetCreditedPlayer(Ship).Should().Be(Ship, "a droid's work earns its pilot the combat point");
            SpaceCrew.GetActingSeat(Ship).Should().Be(ShipCrewSeat.Gunner);

            using (SpaceCrew.BeginStationFire(new SpaceCrewFireContext { Ship = Ship, CrewMember = Player, Seat = ShipCrewSeat.Engineer }))
            {
                SpaceCrew.GetStatSource(Ship).Should().Be(Player);
                SpaceCrew.TryGetDroidAttribute(Player, AbilityType.Perception, out _).Should().BeFalse();
            }

            SpaceCrew.GetStatSource(Ship).Should().Be(Invalid, "the droid's fire resumes");
        }

        SpaceCrew.GetStatSource(Ship).Should().Be(Ship);
        SpaceCrew.IsDroidStatSource(Invalid).Should().BeFalse();
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = System.IO.Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return System.IO.File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
