using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipHudRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
    private const uint Self = 100;
    private const uint Ally = 200;
    private const uint Enemy = 300;

    private static readonly Dictionary<string, ShipModuleDetail> Details = new()
    {
        ["laser"] = new ShipModuleDetail { Name = "Laser Cannon", Type = ShipModuleType.CombatLaser, Activation = ShipModuleActivation.Toggle },
        ["ion"] = new ShipModuleDetail { Name = "Ion Cannon", Type = ShipModuleType.IonCannon, Activation = ShipModuleActivation.Toggle },
        ["torp"] = new ShipModuleDetail { Name = "Torpedo Launcher", Type = ShipModuleType.ProtonBomb, Activation = ShipModuleActivation.OnePress },
        ["mine"] = new ShipModuleDetail { Name = "Mining Laser", Type = ShipModuleType.MiningLaser, Activation = ShipModuleActivation.Toggle },
        ["shield"] = new ShipModuleDetail { Name = "Shield Repairer", Type = ShipModuleType.ShieldRepairer, Activation = ShipModuleActivation.Toggle },
        ["hull"] = new ShipModuleDetail { Name = "Hull Repairer", Type = ShipModuleType.HullRepairer, Activation = ShipModuleActivation.Toggle },
        ["hardener"] = new ShipModuleDetail { Name = "Shield Hardener", Type = ShipModuleType.ShieldHardener, Activation = ShipModuleActivation.Toggle },
    };

    private static ShipModuleDetail Lookup(string tag) => Details.GetValueOrDefault(tag);

    private static string Name(uint id) => id switch
    {
        Ally => "Ally Ship",
        Enemy => "Enemy Ship",
        _ => null,
    };

    private static ShipStatus.ShipStatusModule Module(string tag, double cycleSeconds = 6, double elapsed = 0) => new()
    {
        ItemTag = tag,
        RecastStartedAt = Now.AddSeconds(-elapsed),
        RecastTime = Now.AddSeconds(cycleSeconds - elapsed),
    };

    /// <summary>A Medium ship: two lasers and a torpedo on High, a hardener and a shield repairer on Mid, a hull repairer and a mining laser on Low.</summary>
    private static ShipStatus BuildShip()
    {
        var status = new ShipStatus { Size = ShipSize.Medium };
        status.HighPowerModules[1] = Module("laser");
        status.HighPowerModules[2] = Module("ion");
        status.HighPowerModules[3] = Module("torp");
        status.MidPowerModules[1] = Module("hardener");
        status.MidPowerModules[2] = Module("shield");
        status.LowPowerModules[1] = Module("hull");
        status.LowPowerModules[2] = Module("mine");
        return status;
    }

    private static void TurnOn(ShipStatus status, int slot, uint lockedTarget = OBJECT_INVALID) =>
        status.ActiveModules.Add(new ShipStatus.ShipActiveModule { Slot = slot, NextCycleTime = Now.AddSeconds(3), TargetId = lockedTarget });

    private static List<ShipHudModuleRow> Rows(ShipStatus status, ShipCrewSeat seat = ShipCrewSeat.Pilot, uint target = OBJECT_INVALID) =>
        ShipHudRules.BuildModuleRows(status, seat, Now, Lookup, target, Self, Name);

    /// <summary>Checks the HUD lists only modules that are on, High then Mid then Low.</summary>
    [Test]
    public void BuildModuleRows_ListsOnlyModulesThatAreOnInRowOrder()
    {
        var status = BuildShip();
        TurnOn(status, 12);
        TurnOn(status, 2);
        TurnOn(status, 21);
        TurnOn(status, 1);

        var rows = Rows(status);

        rows.Select(x => x.Slot).Should().Equal(1, 2, 21, 12);
        rows.Select(x => x.Name).Should().Equal("Laser Cannon", "Ion Cannon", "Shield Hardener", "Mining Laser");
        rows.Select(x => x.SlotLabel).Should().Equal("High 1", "High 2", "Mid 1", "Low 2");
    }

    /// <summary>Checks a ship with nothing on lists nothing, and a ship without toggle data does not throw.</summary>
    [Test]
    public void BuildModuleRows_HandlesNothingOn()
    {
        Rows(BuildShip()).Should().BeEmpty();

        var status = BuildShip();
        status.ActiveModules = null;
        Rows(status).Should().BeEmpty();
    }

    /// <summary>Checks a toggle whose slot no longer holds a module is left out.</summary>
    [Test]
    public void BuildModuleRows_SkipsAToggleWithNoFittedModule()
    {
        var status = BuildShip();
        TurnOn(status, 9);
        TurnOn(status, 1);

        Rows(status).Select(x => x.Slot).Should().Equal(1);
    }

    /// <summary>Checks a weapon follows the ship's target, so its row names the current target.</summary>
    [Test]
    public void BuildModuleRows_AWeaponShowsTheCurrentTarget()
    {
        var status = BuildShip();
        TurnOn(status, 1);

        Rows(status, target: Enemy)[0].TargetText.Should().Be("Enemy Ship");
        Rows(status)[0].TargetText.Should().Be(ShipHudRules.NoTargetText);
        Rows(status)[0].CanRetarget.Should().BeFalse();
    }

    /// <summary>Checks a support module shows its locked target, not the ship's current one, and can be retargeted.</summary>
    [Test]
    public void BuildModuleRows_ASupportModuleShowsItsLockedTarget()
    {
        var status = BuildShip();
        TurnOn(status, 22, Ally);
        TurnOn(status, 11, Self);

        var rows = Rows(status, target: Enemy);

        rows.Single(x => x.Slot == 22).TargetText.Should().Be("Ally Ship");
        rows.Single(x => x.Slot == 22).CanRetarget.Should().BeTrue();
        rows.Single(x => x.Slot == 11).TargetText.Should().Be(ShipHudRules.SelfTargetText);
    }

    /// <summary>Checks an area module shows the ship and cannot be retargeted.</summary>
    [Test]
    public void BuildModuleRows_ASelfCenteredModuleHasNoTargetToChange()
    {
        var status = BuildShip();
        TurnOn(status, 21);

        var row = Rows(status, target: Ally).Single();

        row.TargetText.Should().Be(ShipHudRules.AreaTargetText);
        row.CanRetarget.Should().BeFalse();
    }

    /// <summary>Checks only support modules offer Retarget.</summary>
    [TestCase(ShipModuleType.ShieldRepairer, true)]
    [TestCase(ShipModuleType.HullRepairer, true)]
    [TestCase(ShipModuleType.CombatLaser, false)]
    [TestCase(ShipModuleType.MiningLaser, false)]
    [TestCase(ShipModuleType.ShieldHardener, false)]
    [TestCase(ShipModuleType.BulwarkShieldGenerator, false)]
    public void CanRetarget_IsTrueOnlyForLockedModules(ShipModuleType type, bool expected)
    {
        ShipHudRules.CanRetarget(type).Should().Be(expected);
    }

    /// <summary>Checks cycle progress is the share of the cycle that has passed, in 5% steps.</summary>
    [TestCase(0, 0f)]
    [TestCase(3, 0.5f)]
    [TestCase(1.49, 0.25f)]
    [TestCase(6, 1f)]
    [TestCase(9, 1f)]
    [TestCase(-2, 0f)]
    public void GetCycleProgress_IsTheShareOfTheCyclePassed(double elapsed, float expected)
    {
        var start = Now.AddSeconds(-elapsed);

        ShipHudRules.GetCycleProgress(start, start.AddSeconds(6), Now).Should().BeApproximately(expected, 0.001f);
    }

    /// <summary>Checks a cycle with no length reads as no progress.</summary>
    [Test]
    public void GetCycleProgress_NoCycleIsZero()
    {
        ShipHudRules.GetCycleProgress(Now, Now, Now).Should().Be(0f);
    }

    /// <summary>Checks a row's progress comes from its module's cycle.</summary>
    [Test]
    public void BuildModuleRows_ProgressComesFromTheModulesCycle()
    {
        var status = BuildShip();
        status.HighPowerModules[1] = Module("laser", 4, 1);
        TurnOn(status, 1);

        Rows(status).Single().Progress.Should().BeApproximately(0.25f, 0.001f);
    }

    /// <summary>Checks Retarget locks an allied ship, falls back to the ship itself when nothing is selected, and turns an enemy away.</summary>
    [Test]
    public void ChooseRetarget_LocksAnAllyOrSelfAndRefusesAnEnemy()
    {
        ShipHudRules.ChooseRetarget(true, true, Ally, Self, out var error).Should().Be(Ally);
        error.Should().BeEmpty();

        ShipHudRules.ChooseRetarget(false, false, OBJECT_INVALID, Self, out error).Should().Be(Self);
        error.Should().BeEmpty();

        ShipHudRules.ChooseRetarget(true, false, Enemy, Self, out error).Should().Be(OBJECT_INVALID);
        error.Should().Be(ShipHudRules.RetargetNeedsAllyMessage);
    }

    /// <summary>Checks a retargeted lock is what the row then shows.</summary>
    [Test]
    public void Retarget_ChangesWhatTheRowShows()
    {
        var status = BuildShip();
        TurnOn(status, 22, Self);
        Rows(status).Single().TargetText.Should().Be(ShipHudRules.SelfTargetText);

        status.ActiveModules.Single().TargetId = ShipHudRules.ChooseRetarget(true, true, Ally, Self, out _);

        Rows(status).Single().TargetText.Should().Be("Ally Ship");
    }

    /// <summary>Checks All Weapons controls the toggled offensive modules, not one-press weapons, miners or support modules.</summary>
    [Test]
    public void GetWeaponSlots_AreTheToggledOffensiveModules()
    {
        ShipHudRules.GetWeaponSlots(BuildShip(), ShipCrewSeat.Pilot, Lookup).Should().Equal(1, 2);
    }

    /// <summary>Checks a ship with no weapons has none to control.</summary>
    [Test]
    public void GetWeaponSlots_NoShipHasNoWeapons()
    {
        ShipHudRules.GetWeaponSlots(null, ShipCrewSeat.Pilot, Lookup).Should().BeEmpty();
        ShipHudRules.GetWeaponSlots(new ShipStatus(), ShipCrewSeat.Pilot, Lookup).Should().BeEmpty();
    }

    /// <summary>Checks the next press turns the rest on until all are on, then turns them off.</summary>
    [TestCase(0, 2, true)]
    [TestCase(1, 2, true)]
    [TestCase(2, 2, false)]
    [TestCase(0, 0, false)]
    public void ShouldTurnAllWeaponsOn_FollowsHowManyAreOn(int on, int total, bool expected)
    {
        ShipHudRules.ShouldTurnAllWeaponsOn(on, total).Should().Be(expected);
    }

    /// <summary>Checks a module row puts the module and its target on separate lines, so neither cuts the other off.</summary>
    [Test]
    public void GetModuleRowText_PutsTheTargetOnItsOwnLine()
    {
        var row = new ShipHudModuleRow { SlotLabel = "High 1", Name = "Laser Cannon", TargetText = "Septra Khanate Scrap Hauler" };

        ShipHudRules.GetModuleRowText(row).Should().Be("High 1: Laser Cannon" + "\n" + "Target: Septra Khanate Scrap Hauler");
    }

    /// <summary>Checks the All Weapons button reads Off, partial or On.</summary>
    [TestCase(0, 0, "All Weapons: none fitted")]
    [TestCase(0, 3, "All Weapons: Off")]
    [TestCase(2, 3, "All Weapons: 2 of 3 on")]
    [TestCase(3, 3, "All Weapons: On")]
    public void GetAllWeaponsText_ReadsTheState(int on, int total, string expected)
    {
        ShipHudRules.GetAllWeaponsText(on, total).Should().Be(expected);
    }

    /// <summary>Checks the count of weapons that are on ignores other toggles.</summary>
    [Test]
    public void CountOn_CountsOnlyTheGivenWeapons()
    {
        var status = BuildShip();
        TurnOn(status, 1);
        TurnOn(status, 22, Self);

        ShipHudRules.CountOn(status, new[] { 1, 2 }).Should().Be(1);
    }

    private static ShipStatus BuildLargeShipWithStations()
    {
        var status = new ShipStatus
        {
            Size = ShipSize.Large,
            SlotTags = new Dictionary<int, ShipSlotTag>
            {
                [1] = ShipSlotTag.Helm,
                [2] = ShipSlotTag.Helm,
                [3] = ShipSlotTag.Gunner,
                [21] = ShipSlotTag.Engineer,
            },
        };
        status.HighPowerModules[1] = Module("laser");
        status.HighPowerModules[2] = Module("ion");
        status.HighPowerModules[3] = Module("laser");
        status.MidPowerModules[1] = Module("shield");
        return status;
    }

    /// <summary>Checks the one seat filter: each seat sees and controls only its own slots.</summary>
    [Test]
    public void IsSlotInSeat_EachSeatSeesItsOwnSlots()
    {
        var status = BuildLargeShipWithStations();

        ShipHudRules.IsSlotInSeat(status, 1, ShipCrewSeat.Pilot).Should().BeTrue();
        ShipHudRules.IsSlotInSeat(status, 3, ShipCrewSeat.Pilot).Should().BeFalse();
        ShipHudRules.IsSlotInSeat(status, 3, ShipCrewSeat.Gunner).Should().BeTrue();
        ShipHudRules.IsSlotInSeat(status, 21, ShipCrewSeat.Engineer).Should().BeTrue();
        ShipHudRules.IsSlotInSeat(status, 21, ShipCrewSeat.Pilot).Should().BeFalse();
    }

    /// <summary>Checks the module list and All Weapons both go through the seat filter.</summary>
    [Test]
    public void SeatFilter_AppliesToTheModuleListAndToAllWeapons()
    {
        var status = BuildLargeShipWithStations();
        TurnOn(status, 1);
        TurnOn(status, 3);
        TurnOn(status, 21, Self);

        Rows(status, ShipCrewSeat.Pilot).Select(x => x.Slot).Should().Equal(1);
        Rows(status, ShipCrewSeat.Gunner).Select(x => x.Slot).Should().Equal(3);
        Rows(status, ShipCrewSeat.Engineer).Select(x => x.Slot).Should().Equal(21);

        ShipHudRules.GetWeaponSlots(status, ShipCrewSeat.Pilot, Lookup).Should().Equal(1, 2);
        ShipHudRules.GetWeaponSlots(status, ShipCrewSeat.Gunner, Lookup).Should().Equal(3);
        ShipHudRules.GetWeaponSlots(status, ShipCrewSeat.Engineer, Lookup).Should().BeEmpty();
    }

    /// <summary>Checks the stats copied from a status include hardened resistance and a Bulwark shield bonus while they last.</summary>
    [Test]
    public void FromStatus_CopiesPoolsResistancesAndTimedBonuses()
    {
        var status = new ShipStatus
        {
            Hull = 40, MaxHull = 50, Shield = 30, MaxShield = 100, Capacitor = 20, MaxCapacitor = 60,
            Defense = 12, Evasion = 9, SpeedMultiplier = 1.25,
            Resistances = new Dictionary<ResistanceType, int>
            {
                [ResistanceType.Thermal] = 10, [ResistanceType.EM] = 5, [ResistanceType.Explosive] = 0,
            },
            DamageBonusPercent = new Dictionary<CombatDamageType, int> { [CombatDamageType.Thermal] = 15 },
            HardenedAmount = 20, HardenedUntil = Now.AddSeconds(5),
            ShieldAuraPercent = 10, ShieldAuraUntil = Now.AddSeconds(5),
        };
        status.HighPowerModules[1] = Module("laser");
        status.LowPowerModules[1] = Module("hull");
        status.LowPowerModules[2] = Module("mine");

        var stats = ShipHudStats.FromStatus(status, Now);

        stats.Hull.Should().Be(40);
        stats.MaxShield.Should().Be(110);
        stats.ThermalResistance.Should().Be(30);
        stats.EmResistance.Should().Be(25);
        stats.ExplosiveResistance.Should().Be(20);
        stats.ThermalDamagePercent.Should().Be(15);
        stats.EmDamagePercent.Should().Be(0);
        stats.Defense.Should().Be(12);
        stats.SpeedMultiplier.Should().Be(1.25);
        stats.HighFitted.Should().Be(1);
        stats.MidFitted.Should().Be(0);
        stats.LowFitted.Should().Be(2);

        var later = ShipHudStats.FromStatus(status, Now.AddSeconds(10));
        later.MaxShield.Should().Be(100);
        later.ThermalResistance.Should().Be(10);
    }

    /// <summary>Checks the Stats tab shows every stat the plan lists, with the values it was given.</summary>
    [Test]
    public void BuildStatRows_ShowsEveryPlannedStat()
    {
        var rows = ShipHudRules.BuildStatRows(new ShipHudStats
        {
            Hull = 40, MaxHull = 50, Shield = 30, MaxShield = 100, Capacitor = 20, MaxCapacitor = 60,
            ShieldRegenPerSecond = 1.5, CapacitorRegenPerSecond = 2.55,
            EnergyAttack = 21, LauncherAttack = 19, EnergyAccuracy = 55, LauncherAccuracy = 48,
            Defense = 17, Evasion = 33, CriticalRate = 7,
            ThermalResistance = 10, EmResistance = 5, ExplosiveResistance = -5,
            ThermalDamagePercent = 20, EmDamagePercent = 0, ExplosiveDamagePercent = -10,
            SpeedMultiplier = 1.1, Cargo = 20,
            HighFitted = 2, HighSlots = 3, MidFitted = 1, MidSlots = 2, LowFitted = 0, LowSlots = 2,
        });

        string Value(string label) => rows.Single(x => x.Label == label).Value;

        Value("Hull").Should().Be("40 / 50");
        Value("Shield").Should().Be("30 / 100");
        Value("Shield Regen").Should().Be("1.5 / s");
        Value("Capacitor").Should().Be("20 / 60");
        Value("Capacitor Regen").Should().Be("2.55 / s");
        Value("Attack (energy)").Should().Be("21");
        Value("Attack (launcher)").Should().Be("19");
        Value("Accuracy (energy)").Should().Be("55");
        Value("Accuracy (launcher)").Should().Be("48");
        Value("Defense").Should().Be("17");
        Value("Evasion").Should().Be("33");
        Value("Critical Rate").Should().Be("7%");
        Value("Thermal Resistance").Should().Be("10%");
        Value("Explosive Resistance").Should().Be("-5%");
        Value("Thermal Damage").Should().Be("+20%");
        Value("EM Damage").Should().Be("0%");
        Value("Explosive Damage").Should().Be("-10%");
        Value("Speed").Should().Be("110%");
        Value("Cargo").Should().Be("0 / 20");
        Value("High Slots").Should().Be("2 / 3");
        Value("Mid Slots").Should().Be("1 / 2");
        Value("Low Slots").Should().Be("0 / 2");
    }

    /// <summary>Checks the Stats tab shows the Engineer's power routing and the speed it gives.</summary>
    [Test]
    public void StatsTab_ShowsThePowerRoutingAndTheRoutedSpeed()
    {
        var status = new ShipStatus { SpeedMultiplier = 1.0, PowerRouting = ShipPowerRouting.Engines };
        var stats = ShipHudStats.FromStatus(status, Now);
        stats.PowerRouting.Should().Be(ShipPowerRouting.Engines);
        stats.SpeedMultiplier.Should().BeApproximately(1.2, 1e-9);

        var rows = ShipHudRules.BuildStatRows(stats);
        rows.Single(x => x.Label == "Power Routing").Value.Should().Be("Engines");
        rows.Single(x => x.Label == "Speed").Value.Should().Be("120%");
        ShipHudRules.BuildStatRows(new ShipHudStats()).Single(x => x.Label == "Power Routing").Value.Should().Be("Balanced");
    }

    /// <summary>Checks a hull with no listed cargo shows a dash.</summary>
    [Test]
    public void BuildStatRows_NoCargoShowsADash()
    {
        ShipHudRules.BuildStatRows(new ShipHudStats()).Single(x => x.Label == "Cargo").Value.Should().Be("-");
    }

    /// <summary>Checks the Stats tab shows the cargo bay filled against the hull's Cargo, then each commodity aboard.</summary>
    [Test]
    public void BuildStatRows_ShowCargoUsedAndTheCommoditiesAboard()
    {
        var rows = ShipHudRules.BuildStatRows(new ShipHudStats
        {
            Cargo = 480,
            CargoUsed = 130,
            CargoLines = new List<(string Name, int Units)> { ("Grain", 40), ("Kinrath Silk", 50) },
        });

        rows.Single(x => x.Label == "Cargo").Value.Should().Be("130 / 480");
        var cargo = rows.FindIndex(x => x.Label == "Cargo");
        rows[cargo + 1].Label.Should().Be("Grain");
        rows[cargo + 1].Value.Should().Be("40");
        rows[cargo + 2].Label.Should().Be("Kinrath Silk");
        rows.Should().HaveCount(cargo + 3);
    }

    /// <summary>Checks the All Weapons feat, its window id and its hak data are consistent.</summary>
    [Test]
    public void AllWeaponsFeat_HasItsEnumRowAndTextEntries()
    {
        ((int)FeatType.ShipAllWeapons).Should().Be(3350);
        Enum.GetValues<GuiWindowType>().Count(x => (int)x == (int)GuiWindowType.ShipHud).Should().Be(1);

        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(directory!.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        rows[3350]["LABEL"].Should().Be("ShipAllWeapons");
        rows[3350]["FEAT"].Should().Be("16970348");
        rows[3350]["DESCRIPTION"].Should().Be("16970349");
        rows[3350]["TARGETSELF"].Should().Be("1");
        rows[3350]["SPELLID"].Should().Be("****");

        var icon = rows[3350]["ICON"];
        foreach (var file in new[] { icon, "pr0_allweap", "pr5_allweap" })
        {
            File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_ability", file + ".tga")).Should().BeTrue(file);
        }
    }

    /// <summary>Checks the pilot's HUD counts every module that is on, including the ones a crew seat or droid runs (null seat).</summary>
    [Test]
    public void BuildModuleRows_WithNoSeat_ListsEveryActiveModuleAndNamesTheCrewSeat()
    {
        var status = BuildLargeShipWithStations();
        TurnOn(status, 1);
        TurnOn(status, 3);
        TurnOn(status, 21);

        var all = ShipHudRules.BuildModuleRows(status, null, Now, Lookup, OBJECT_INVALID, Self, Name);

        all.Select(x => x.Slot).Should().Equal(1, 3, 21);
        all.Select(x => x.SeatLabel).Should().Equal(null, "Gunner", "Engineer");
        ShipHudRules.GetModuleRowText(all[0]).Should().StartWith("High 1: Laser Cannon\n");
        ShipHudRules.GetModuleRowText(all[1]).Should().StartWith("High 3: Laser Cannon (Gunner)\n");
        ShipHudRules.GetModuleRowText(all[2]).Should().StartWith("Mid 1: Shield Repairer (Engineer)\n");
    }

    /// <summary>Checks a crew member's own HUD (a seat given) still lists only that seat's modules.</summary>
    [Test]
    public void BuildModuleRows_WithASeat_StillListsOnlyThatSeat()
    {
        var status = BuildLargeShipWithStations();
        TurnOn(status, 1);
        TurnOn(status, 3);
        TurnOn(status, 21);

        Rows(status, ShipCrewSeat.Pilot).Select(x => x.Slot).Should().Equal(1);
        Rows(status, ShipCrewSeat.Gunner).Select(x => x.Slot).Should().Equal(3);
        Rows(status, ShipCrewSeat.Gunner).Single().SeatLabel.Should().BeNull();
    }
}
