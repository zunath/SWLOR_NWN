using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The revamp ship stat rules (Design Bible "Starships (Revamp)", ship stat rules). The numbers are worked out by hand.
/// </summary>
public class ShipStatCalculatorTests
{
    private static ShipHullEntry Hull(ShipSize size = ShipSize.Small, Action<ShipHullEntry>? change = null)
    {
        var hull = new ShipHullEntry
        {
            Tag = "sd_test",
            Name = "Test",
            Size = size,
            High = 4,
            Mid = 4,
            Low = 4,
            Hull = 100,
            Shield = 80,
            Capacitor = 60,
            CapacitorRegen = 2.5,
            Speed = 1.0,
            Attack = 8,
            Accuracy = 8,
            Defense = 8,
            Evasion = 12,
        };
        change?.Invoke(hull);
        return hull;
    }

    private static ShipFittedModule Fit(ShipModulePowerType row, int slot, Action<ShipModuleStep> step)
    {
        var module = new ShipModuleStep { Tag = "sm_test", Row = row };
        step(module);
        return new ShipFittedModule { Row = row, Slot = slot, Step = module };
    }

    private static ShipStatResult Calculate(ShipHullEntry hull, int wil = 10, params ShipFittedModule[] modules) =>
        ShipStatCalculator.Calculate(hull, modules, wil);

    [Test]
    public void BareHull_UsesTheHullStatsAndAddsTwoCapacitorPerWillpower()
    {
        var hull = Hull(change: h => { h.ResistAll = 7; h.Attack = 9; });

        var result = Calculate(hull, wil: 12);

        result.MaxHull.Should().Be(100);
        result.MaxShield.Should().Be(80);
        result.MaxCapacitor.Should().Be(60 + 2 * 12);
        result.CapacitorRegenPerSecond.Should().Be(2.5);
        result.Attack.Should().Be(9);
        result.Accuracy.Should().Be(8);
        result.Defense.Should().Be(8);
        result.Evasion.Should().Be(12);
        result.Size.Should().Be(ShipSize.Small);
        result.CapitalShip.Should().BeFalse();
        result.Resistances[ResistanceType.Thermal].Should().Be(7);
        result.Resistances[ResistanceType.EM].Should().Be(7);
        result.Resistances[ResistanceType.Explosive].Should().Be(7);
    }

    [TestCase(ShipSize.Small, 100)]
    [TestCase(ShipSize.Medium, 110)]
    [TestCase(ShipSize.Large, 110)]
    [TestCase(ShipSize.Capital, 110)]
    public void ModuleOutput_ScalesWithHullSizeOnHullShieldCapacitorAndRegen(ShipSize size, int expected)
    {
        var hull = Hull(size, h => { h.Hull = 0; h.Shield = 0; h.Capacitor = 0; h.CapacitorRegen = 0; });

        var result = Calculate(hull, wil: 0,
            Fit(ShipModulePowerType.Low, 1, s => s.Hull = 100),
            Fit(ShipModulePowerType.Mid, 1, s => { s.Shield = 100; s.MaxCapacitor = 100; }),
            Fit(ShipModulePowerType.Low, 2, s => s.CapacitorRegen = 1.0));

        result.MaxHull.Should().Be(expected);
        result.MaxShield.Should().Be(expected);
        result.MaxCapacitor.Should().Be(expected);
        result.CapacitorRegenPerSecond.Should().BeApproximately(expected / 100.0, 0.0001);
    }

    [Test]
    public void ModulesOfTheSameKind_StackHullShieldAndCapacitor()
    {
        var result = Calculate(Hull(), 10,
            Fit(ShipModulePowerType.Low, 1, s => s.Hull = 40),
            Fit(ShipModulePowerType.Low, 2, s => s.Hull = 40),
            Fit(ShipModulePowerType.Mid, 1, s => s.Shield = 30),
            Fit(ShipModulePowerType.Mid, 2, s => s.Shield = 30));

        result.MaxHull.Should().Be(100 + 80);
        result.MaxShield.Should().Be(80 + 60);
    }

    [Test]
    public void OnlyTheBestPlatingDefense_Counts_ButTheirHullStacks()
    {
        var result = Calculate(Hull(), 10,
            Fit(ShipModulePowerType.Low, 1, s => { s.Hull = 40; s.ModuleDefense = 18; }),
            Fit(ShipModulePowerType.Low, 2, s => { s.Hull = 60; s.ModuleDefense = 45; }),
            Fit(ShipModulePowerType.Low, 3, s => { s.Hull = 20; s.ModuleDefense = 9; }));

        result.Defense.Should().Be(8 + 45);
        result.MaxHull.Should().Be(100 + 40 + 60 + 20);
    }

    [Test]
    public void OnlyTheBestThrustersEvasion_Counts_ButSpeedStacks()
    {
        var result = Calculate(Hull(change: h => h.Speed = 1.0), 10,
            Fit(ShipModulePowerType.Low, 1, s => { s.ModuleEvasion = 30; s.SpeedPercent = 5; }),
            Fit(ShipModulePowerType.Low, 2, s => { s.ModuleEvasion = 50; s.SpeedPercent = 5; }),
            Fit(ShipModulePowerType.Low, 3, s => { s.ModuleEvasion = 10; s.SpeedPercent = 5; }));

        result.Evasion.Should().Be(12 + 50);
        result.Speed.Should().BeApproximately(1.15, 0.0001);
    }

    [Test]
    public void AdaptivePlatingEvasionPenalty_AlwaysApplies()
    {
        var result = Calculate(Hull(), 10,
            Fit(ShipModulePowerType.Low, 1, s => s.ModuleEvasion = 40),
            Fit(ShipModulePowerType.Low, 2, s => { s.ModuleEvasion = -4; s.ResistAll = 6; }),
            Fit(ShipModulePowerType.Low, 3, s => { s.ModuleEvasion = -4; s.ResistAll = 6; }));

        result.Evasion.Should().Be(12 + 40 - 8);
        result.Resistances[ResistanceType.Thermal].Should().Be(12);
    }

    [Test]
    public void OnlyTheBestTargetingAccuracy_Counts()
    {
        var result = Calculate(Hull(), 10,
            Fit(ShipModulePowerType.Mid, 1, s => s.ModuleAccuracy = 10),
            Fit(ShipModulePowerType.Mid, 2, s => s.ModuleAccuracy = 20),
            Fit(ShipModulePowerType.Low, 1, s => s.ModuleAccuracy = 22));

        result.Accuracy.Should().Be(8 + 22);
    }

    [Test]
    public void HullPercentChanges_ApplyToTheTotalsWithModulesAndWillpower()
    {
        var hull = Hull(change: h =>
        {
            h.HullPercent = 10;
            h.ShieldPercent = -10;
            h.CapacitorPercent = 20;
        });

        var result = Calculate(hull, wil: 10,
            Fit(ShipModulePowerType.Low, 1, s => s.Hull = 100),
            Fit(ShipModulePowerType.Mid, 1, s => s.Shield = 20),
            Fit(ShipModulePowerType.Mid, 2, s => s.MaxCapacitor = 20));

        result.MaxHull.Should().Be((int)Math.Round((100 + 100) * 1.1));
        result.MaxShield.Should().Be((int)Math.Round((80 + 20) * 0.9));
        result.MaxCapacitor.Should().Be((int)Math.Round((60 + 20 + 20) * 1.2));
    }

    [Test]
    public void HullShieldRegenPercentAndCriticalBonus_PassThrough()
    {
        var result = Calculate(Hull(change: h => { h.ShieldRegenPercent = 25; h.CriticalBonus = 13; }));

        result.ShieldRegenBonusPercent.Should().Be(25);
        result.CriticalBonus.Should().Be(13);
    }

    [Test]
    public void Resistances_AddAllTypeAndPerTypePlating()
    {
        var hull = Hull(change: h => h.ResistAll = 5);

        var result = Calculate(hull, 10,
            Fit(ShipModulePowerType.Low, 1, s => s.ResistThermal = 10),
            Fit(ShipModulePowerType.Low, 2, s => s.ResistThermal = 10),
            Fit(ShipModulePowerType.Low, 3, s => s.ResistEM = 16),
            Fit(ShipModulePowerType.Low, 4, s => s.ResistAll = 6));

        result.Resistances[ResistanceType.Thermal].Should().Be(5 + 6 + 20);
        result.Resistances[ResistanceType.EM].Should().Be(5 + 6 + 16);
        result.Resistances[ResistanceType.Explosive].Should().Be(5 + 6);
    }

    [Test]
    public void ShieldHardener_AddsNoPassiveResistance()
    {
        var result = Calculate(Hull(), 10,
            Fit(ShipModulePowerType.Mid, 1, s => { s.Family = ShipModuleFamily.ShieldHardener; s.ResistAll = 19; }));

        result.Resistances[ResistanceType.Thermal].Should().Be(0);
    }

    [Test]
    public void DamageBonus_AddsAmplifiersWeaponsComputerAndHullPercent()
    {
        var hull = Hull(change: h => { h.DamagePercent = 10; h.ExplosivePercent = 5; });

        var result = Calculate(hull, 10,
            Fit(ShipModulePowerType.Low, 1, s => s.DamageThermal = 8),
            Fit(ShipModulePowerType.Low, 2, s => s.DamageThermal = 8),
            Fit(ShipModulePowerType.Low, 3, s => s.DamageEM = 6),
            Fit(ShipModulePowerType.Low, 4, s => s.DamageAll = 10));

        result.DamageBonusPercent[CombatDamageType.Thermal].Should().Be(10 + 16 + 10);
        result.DamageBonusPercent[CombatDamageType.EM].Should().Be(10 + 6 + 10);
        result.DamageBonusPercent[CombatDamageType.Explosive].Should().Be(10 + 5 + 10);
    }

    [Test]
    public void Speed_IsHullSpeedTimesTheSpeedPercentSum()
    {
        var result = Calculate(Hull(change: h => h.Speed = 1.1), 10,
            Fit(ShipModulePowerType.Low, 1, s => s.SpeedPercent = 5),
            Fit(ShipModulePowerType.Low, 2, s => s.SpeedPercent = 5));

        result.Speed.Should().BeApproximately(1.21, 0.0001);
    }

    [Test]
    public void SlotTags_FillHelmFirstThenStationsPerRow()
    {
        var hull = Hull(ShipSize.Large, h =>
        {
            h.High = 4; h.StationHigh = 2;
            h.Mid = 3; h.StationMid = 1;
            h.Low = 4; h.StationLow = 2;
        });

        var tags = ShipStatCalculator.BuildSlotTags(hull);

        tags.Should().HaveCount(11);
        tags[1].Should().Be(ShipSlotTag.Helm);
        tags[2].Should().Be(ShipSlotTag.Helm);
        tags[3].Should().Be(ShipSlotTag.Gunner);
        tags[4].Should().Be(ShipSlotTag.Gunner);
        tags[21].Should().Be(ShipSlotTag.Helm);
        tags[22].Should().Be(ShipSlotTag.Helm);
        tags[23].Should().Be(ShipSlotTag.Engineer);
        tags[11].Should().Be(ShipSlotTag.Helm);
        tags[12].Should().Be(ShipSlotTag.Helm);
        tags[13].Should().Be(ShipSlotTag.Engineer);
        tags[14].Should().Be(ShipSlotTag.Engineer);
    }

    [Test]
    public void SlotTags_LastMidStationOnACapitalHullIsSensors()
    {
        var hull = Hull(ShipSize.Capital, h =>
        {
            h.Mid = 5; h.StationMid = 3;
            h.High = 0; h.Low = 0;
        });

        var tags = ShipStatCalculator.BuildSlotTags(hull);

        tags[21].Should().Be(ShipSlotTag.Helm);
        tags[22].Should().Be(ShipSlotTag.Helm);
        tags[23].Should().Be(ShipSlotTag.Engineer);
        tags[24].Should().Be(ShipSlotTag.Engineer);
        tags[25].Should().Be(ShipSlotTag.Sensors);
    }

    [Test]
    public void SlotTags_AHullWithoutStationsIsAllHelm()
    {
        var tags = ShipStatCalculator.BuildSlotTags(Hull());

        tags.Values.Should().OnlyContain(tag => tag == ShipSlotTag.Helm);
        tags.Should().HaveCount(12);
    }

    [Test]
    public void StationSlotModules_ContributeNothingWithoutCrew()
    {
        var hull = Hull(ShipSize.Large, h => { h.StationLow = 1; h.StationMid = 1; });

        var result = Calculate(hull, wil: 0,
            Fit(ShipModulePowerType.Low, 1, s => { s.Hull = 50; s.ModuleDefense = 10; s.DamageAll = 5; s.SpeedPercent = 5; }),
            // Low slot 4 is the station slot on this hull.
            Fit(ShipModulePowerType.Low, 4, s => { s.Hull = 500; s.ModuleDefense = 99; s.DamageAll = 50; s.SpeedPercent = 50; s.ModuleEvasion = -20; }),
            // Mid slot 4 is the station slot.
            Fit(ShipModulePowerType.Mid, 4, s => { s.Shield = 500; s.ModuleAccuracy = 99; }));

        result.MaxHull.Should().Be(100 + (int)Math.Round(50 * 1.1));
        result.MaxShield.Should().Be(80);
        result.Defense.Should().Be(8 + 10);
        result.Accuracy.Should().Be(8);
        result.Evasion.Should().Be(12);
        result.Speed.Should().BeApproximately(1.05, 0.0001);
        result.DamageBonusPercent[CombatDamageType.Thermal].Should().Be(5);
    }

    [Test]
    public void NullModulesAndMissingSteps_AreIgnored()
    {
        var result = ShipStatCalculator.Calculate(Hull(), new ShipFittedModule?[]
        {
            null,
            new ShipFittedModule { Row = ShipModulePowerType.Low, Slot = 1, Step = null },
            Fit(ShipModulePowerType.Low, 2, s => s.Hull = 10)
        }!, 0);

        result.MaxHull.Should().Be(110);
    }

    [Test]
    public void Apply_FillsOnRegistrationAndClampsOtherwise()
    {
        var hull = Hull();
        var result = Calculate(hull, wil: 0);
        var status = new ShipStatus { Hull = 5, Shield = 5, Capacitor = 5 };

        ShipStatCalculator.Apply(result, status, fill: true);
        status.Hull.Should().Be(100);
        status.Shield.Should().Be(80);
        status.Capacitor.Should().Be(60);

        var bigger = Calculate(hull, wil: 0, Fit(ShipModulePowerType.Low, 1, s => s.Hull = 100));
        ShipStatCalculator.Apply(bigger, status, fill: false);
        status.MaxHull.Should().Be(200);
        status.Hull.Should().Be(100, "adding a module raises the maximum but not the current hull");

        ShipStatCalculator.Apply(result, status, fill: false);
        status.MaxHull.Should().Be(100);
        status.Hull.Should().Be(100);

        status.Hull = 100;
        var smaller = Calculate(Hull(change: h => h.Hull = 40), wil: 0);
        ShipStatCalculator.Apply(smaller, status, fill: false);
        status.Hull.Should().Be(40, "removing a module limits the current hull to the new maximum");
    }

    [Test]
    public void Apply_CopiesTheDerivedStatsOntoTheStatus()
    {
        var hull = Hull(ShipSize.Capital, h =>
        {
            h.ResistAll = 3; h.Speed = 0.6; h.Evasion = -12; h.ShieldRegenPercent = 10; h.CriticalBonus = 4;
            h.StationHigh = 2;
        });
        var status = new ShipStatus();

        ShipStatCalculator.Apply(Calculate(hull), status, fill: true);

        status.Size.Should().Be(ShipSize.Capital);
        status.CapitalShip.Should().BeTrue();
        status.Evasion.Should().Be(-12);
        status.SpeedMultiplier.Should().BeApproximately(0.6, 0.0001);
        status.ShieldRegenBonusPercent.Should().Be(10);
        status.CriticalBonus.Should().Be(4);
        status.Resistances[ResistanceType.Explosive].Should().Be(3);
        status.SlotTags[3].Should().Be(ShipSlotTag.Gunner);
        status.CapacitorRegenPerSecond.Should().Be(2.5);
    }

    [TestCase(ShipSize.Small, 1.0)]
    [TestCase(ShipSize.Medium, 1.1)]
    [TestCase(ShipSize.Large, 1.35)]
    [TestCase(ShipSize.Capital, 1.35)]
    public void WeaponSizeMultiplier_MatchesTheBibleSizeRules(ShipSize size, double expected)
    {
        ShipStatCalculator.WeaponSizeMultiplier(size).Should().Be(expected);
    }

    [TestCase(ShipModulePowerType.High, 3, 3)]
    [TestCase(ShipModulePowerType.Low, 3, 13)]
    [TestCase(ShipModulePowerType.Mid, 3, 23)]
    public void HotbarSlot_FollowsTheHighLowMidFeatBlocks(ShipModulePowerType row, int slot, int expected)
    {
        ShipStatCalculator.GetHotbarSlot(row, slot).Should().Be(expected);
    }
}
