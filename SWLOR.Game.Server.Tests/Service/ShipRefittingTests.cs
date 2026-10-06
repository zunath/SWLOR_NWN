using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipRefittingTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Dictionary<SkillType, int> Novice = new() { [SkillType.Piloting] = 1 };
    private static ShipStatus Ship() => ShipFittingConversion.Convert(new() {
        ItemTag = "ShipDeedLightFreighter", MaxHull = 50, Hull = 20,
        MaxShield = 50, Shield = 10, MaxCapacitor = 40, Capacitor = 10 });
    private static ShipStatus.ShipStatusModule Item(string design, string id) => new() {
        Design = design, ItemTag = design, ItemInstanceId = id, SerializedItem = "saved:" + id };

    [Test]
    public void FittingAndRemoval_AreDetachedPreserveDeficitsAndPrepareForFiveSeconds()
    {
        var original = Ship();
        var fitted = ShipRefitting.Equip(original, ShipFittingBank.Low, 1, Item("hull_plating", "plate"), Novice, Now);
        original.LowPowerModules.Should().BeEmpty();
        fitted.MaxHull.Should().Be(200); fitted.Hull.Should().Be(170);
        fitted.RefitReadyAt.Should().Be(Now.AddSeconds(5));
        var removed = ShipRefitting.Remove(fitted, ShipFittingBank.Low, 1, Now, Novice);
        fitted.LowPowerModules.Should().ContainKey(1);
        removed.LowPowerModules.Should().BeEmpty();
        removed.Hull.Should().Be(110);
        removed.RefitRecovery["plate"].SerializedItem.Should().Be("saved:plate");
        removed.ResourceDeficits.Should().Be(original.ResourceDeficits);
    }

    [Test]
    public void InvalidFit_DoesNotConsumeTheItemOrMutateTheShip()
    {
        var ship = Ship();
        var action = () => ShipRefitting.Equip(ship, ShipFittingBank.High, 1, Item("heavy_beam", "beam"), Novice, Now);
        action.Should().Throw<InvalidOperationException>().WithMessage("*mount*");
        ship.HighPowerModules.Should().BeEmpty(); ship.RefitRecovery.Should().BeEmpty();
        var wrongBank = () => ShipRefitting.Equip(ship, ShipFittingBank.Low, 1, Item("tracking_laser", "laser"), Novice, Now);
        wrongBank.Should().Throw<InvalidOperationException>().WithMessage("*bank*");
    }

    [Test]
    public void CalibratedFit_UsesCompletePowerBudgetRatherThanIndependentSlotChecks()
    {
        var ranks = new Dictionary<SkillType, int>(Novice) { [SkillType.SpaceIndustry] = 20 };
        var drill = Item("compact_drill", "drill"); drill.Calibration = "Compact";
        var scanner = Item("deep_scanner", "scanner"); scanner.Calibration = "Compact";
        var repair = Item("hull_repair", "repair"); repair.Calibration = "Compact";
        var ship = ShipRefitting.Equip(Ship(), ShipFittingBank.High, 1, drill, ranks, Now);
        ship = ShipRefitting.Equip(ship, ShipFittingBank.Low, 1, scanner, ranks, Now);
        ship = ShipRefitting.Equip(ship, ShipFittingBank.Low, 2, repair, ranks, Now);
        ship.FittingPowerUsed.Should().Be(37);
        var overBudget = () => ShipRefitting.Equip(ship, ShipFittingBank.High, 2, Item("tracking_laser", "laser"), ranks, Now);
        overBudget.Should().Throw<InvalidOperationException>().WithMessage("*power 45 exceeds 40*");
        ship.FittingPowerUsed.Should().Be(37); ship.HighPowerModules.Should().HaveCount(1);
    }

    [Test]
    public void RecoveredItem_CannotBeInstalledAgainUntilOwnershipTransferCompletes()
    {
        var fitted = ShipRefitting.Equip(Ship(), ShipFittingBank.Low, 1, Item("hull_plating", "plate"), Novice, Now);
        var removed = ShipRefitting.Remove(fitted, ShipFittingBank.Low, 1, Now, Novice);
        var action = () => ShipRefitting.Equip(removed, ShipFittingBank.Low, 1, Item("hull_plating", "plate"), Novice, Now);
        action.Should().Throw<InvalidOperationException>().WithMessage("*belongs*");
        removed.RefitRecovery.Should().HaveCount(1);
    }

    [Test]
    public void Configurations_UseOneSlotAndContributeToPowerWithoutGivingFreeResources()
    {
        var ship = Ship();
        var configured = ShipRefitting.Equip(ship, ShipFittingBank.Configuration, 1,
            Item("cargo_conversion", "configuration"), Novice, Now);
        configured.ConfigurationDesign.Should().Be("cargo_conversion");
        configured.FittingPowerUsed.Should().Be(6);
        configured.Shield.Should().Be(configured.MaxShield - 40);
        var second = () => ShipRefitting.Equip(configured, ShipFittingBank.Configuration, 2,
            Item("survey_conversion", "other"), Novice, Now);
        second.Should().Throw<InvalidOperationException>().WithMessage("*unavailable*");
    }
}
