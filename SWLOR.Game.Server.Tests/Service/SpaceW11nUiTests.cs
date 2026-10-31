using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// W11n live-pass fixes: the pilot's quickbar keeps only pilot-seat slots, a droid at the Sensors seat runs the Data
/// Analyzer, and a posted droid's Industry follows its controller.
/// </summary>
public class SpaceW11nUiTests
{
    private static ShipStatus.ShipStatusModule Module(string tag) => new() { ItemTag = tag };

    private static ShipStatus LargeShip()
    {
        var status = new ShipStatus
        {
            Size = ShipSize.Large,
            SlotTags = new Dictionary<int, ShipSlotTag>
            {
                [1] = ShipSlotTag.Helm,
                [2] = ShipSlotTag.Helm,
                [3] = ShipSlotTag.Gunner,
                [11] = ShipSlotTag.Helm,
                [21] = ShipSlotTag.Engineer,
                [22] = ShipSlotTag.Helm,
            },
        };
        status.HighPowerModules[1] = Module("laser");
        status.HighPowerModules[2] = Module("passive");
        status.HighPowerModules[3] = Module("laser");
        status.LowPowerModules[1] = Module("hull");
        status.MidPowerModules[1] = Module("shield");
        status.MidPowerModules[2] = Module("hardener");
        return status;
    }

    private static bool TakesSlot(string tag) => tag != "passive";

    // Pilot hotbar

    [Test]
    public void PilotHotBar_HoldsOnlyPilotSeatSlots_InHighLowMidOrder()
    {
        var slots = PilotHotBarRules.GetPilotSlots(LargeShip(), TakesSlot);

        slots.Should().Equal(1, 11, 22);
    }

    [Test]
    public void PilotHotBar_LeavesOutCrewSlots_AndTheyAreRecognisedAsCrewSlots()
    {
        var status = LargeShip();

        PilotHotBarRules.IsCrewSlot(status, 3).Should().BeTrue("a Gunner slot");
        PilotHotBarRules.IsCrewSlot(status, 21).Should().BeTrue("an Engineer slot");
        PilotHotBarRules.IsCrewSlot(status, 1).Should().BeFalse();
        PilotHotBarRules.GetPilotSlots(status, TakesSlot).Should().NotContain(new[] { 3, 21 });
    }

    [Test]
    public void PilotHotBar_ASmallShipWithNoStationsKeepsEveryModule()
    {
        var status = new ShipStatus { Size = ShipSize.Small };
        status.HighPowerModules[1] = Module("laser");
        status.MidPowerModules[1] = Module("shield");

        PilotHotBarRules.GetPilotSlots(status, TakesSlot).Should().Equal(1, 21);
        PilotHotBarRules.GetPilotSlots(null, TakesSlot).Should().BeEmpty();
    }

    // Droid slicing

    [Test]
    public void ADroid_PressesTheDataAnalyzer_ThoughItIsAOnePressModule()
    {
        DroidCrewRules.GetTask(ShipModuleType.DataAnalyzer, ShipModuleActivation.OnePress).Should().Be(DroidCrewTask.Slicing);
        DroidCrewRules.GetTask(ShipModuleType.CombatLaser, ShipModuleActivation.OnePress).Should().Be(DroidCrewTask.None);
        DroidCrewRules.GetTask(ShipModuleType.CapitalEwar, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.ElectronicWarfare);
    }

    [TestCase(1, 1, false, "")]
    [TestCase(3, 2, false, "")]
    [TestCase(5, 5, false, "")]
    [TestCase(2, 3, false, "too basic")]
    [TestCase(5, 1, true, "in combat")]
    public void ADroid_SlicesATargetOfItsTierOrLower_AndNeverInCombat(int droidTier, int targetTier, bool inCombat, string contains)
    {
        var error = DroidCrewRules.GetSlicingError(droidTier, targetTier, inCombat);

        if (contains.Length == 0)
            error.Should().BeEmpty();
        else
            error.Should().Contain(contains);
    }

    [TestCase(0.25, 24, true)]
    [TestCase(0.25, 25, false)]
    [TestCase(0.75, 74, true)]
    [TestCase(0.75, 75, false)]
    [TestCase(1.05, 84, true)]
    [TestCase(1.05, 85, false)]
    [TestCase(0.0, 0, false)]
    public void ADroidsSlicingChance_IsItsStationEffectiveness(double effectiveness, int roll, bool succeeds)
    {
        DroidCrewRules.SlicingSucceeds(effectiveness, roll).Should().Be(succeeds);
    }

    // Posted droid Industry

    private static ShipDroidCrew Posting() => new()
    {
        Seat = ShipCrewSeat.Engineer, DroidItemId = "d1", Tier = 3, Industry = 40,
        Protocols = new Dictionary<PerkType, int> { [PerkType.EfficiencyProtocol] = 1 },
    };

    [Test]
    public void ARefreshedShipPosting_TakesTheControllersCurrentIndustryTierAndProtocols()
    {
        var posting = Posting();
        var protocols = new Dictionary<PerkType, int> { [PerkType.EfficiencyProtocol] = 2 };

        DroidCrewRules.RefreshPosting(posting, 4, 75, protocols).Should().BeTrue();

        posting.Tier.Should().Be(4);
        posting.Industry.Should().Be(75);
        posting.Protocols[PerkType.EfficiencyProtocol].Should().Be(2);
    }

    [Test]
    public void ARefreshedShipPosting_ReportsNoChangeWhenTheRatingIsTheSame()
    {
        var posting = Posting();
        var same = new Dictionary<PerkType, int> { [PerkType.EfficiencyProtocol] = 1 };

        DroidCrewRules.RefreshPosting(posting, 3, 40, same).Should().BeFalse();
        DroidCrewRules.RefreshPosting(posting, 3, -5, same).Should().BeTrue("a negative Industry reads as 0");
        posting.Industry.Should().Be(0);
    }

    [Test]
    public void ARefreshedHoldingDroid_WorksAtTheNewIndustryAndEfficiency()
    {
        var holding = new PlanetaryHolding { DroidItemId = "d1", DroidIndustry = 20, DroidEfficiencyPercent = 0 };
        var before = HoldingRules.GetDroidPercent(holding);

        HoldingRules.RefreshDroid(holding, 80, 10).Should().BeTrue();

        holding.DroidIndustry.Should().Be(80);
        holding.DroidEfficiencyPercent.Should().Be(10);
        HoldingRules.GetDroidPercent(holding).Should().BeGreaterThan(before);
        HoldingRules.RefreshDroid(holding, 80, 10).Should().BeFalse();
    }

    [Test]
    public void ARefreshedHolding_WithNoDroid_IsLeftAlone()
    {
        var holding = new PlanetaryHolding();

        HoldingRules.RefreshDroid(holding, 80, 10).Should().BeFalse();
        holding.DroidIndustry.Should().Be(0);
    }

    // Wiring

    private static string ReadServerSource(params string[] path)
    {
        var directory = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = System.IO.Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return System.IO.File.ReadAllText(full).Replace("\r\n", "\n");
    }

    [Test]
    public void ThePilotsHud_ListsTheWholeShip_AndTheLaunchKeepsCrewSlotsOffTheQuickbar()
    {
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipHudViewModel.cs")
            .Should().Contain("SpaceHud.GetModuleRows(Player, status, null)");

        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("PilotHotBarRules.GetPilotSlots(");
        space.Should().Contain("RemoveCrewSlotsFromHotBar(player, dbPlayerShip.Status)");
    }

    [Test]
    public void TheDataAnalyzer_ValidatesAndRunsAsTheDroidNotThePilot()
    {
        var activation = ReadServerSource("Service", "Space.cs");
        activation.IndexOf("using var crewFire = SpaceCrew.BeginStationFire", System.StringComparison.Ordinal).Should()
            .BeLessThan(activation.IndexOf("shipModuleDetails.ModuleValidationAction(", System.StringComparison.Ordinal),
                "the module is validated inside the station's fire, so a droid is not judged as the pilot");

        var slicing = ReadServerSource("Service", "SpaceSlicing.cs");
        slicing.Should().Contain("GetDroidSlicingError(ship, target, source, tier, droid)");
        slicing.Should().Contain("DroidCrewRules.SlicingSucceeds(");
        slicing.Should().Contain("OnSlicingTargetDestroyed(target, droidSource);", "a droid's miss is a failed slice, as for a player");

        ReadServerSource("Service", "SpaceDroidCrew.cs").Should().Contain("RunDroidDataAnalyzer(pilot, slots, posted, target)");
    }

    [Test]
    public void APostedDroid_IsRefreshedWhenItsInstructionsChange_AndAtLogin()
    {
        ReadServerSource("Service", "Droid.cs").Should().Contain("Space.RefreshPostedDroid(controller)");
        ReadServerSource("Service", "SpaceDroidCrew.cs").Should().Contain("RefreshPostedDroidsAtLogin");
    }
}
