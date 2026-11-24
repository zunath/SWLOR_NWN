using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The Cargo Expander module family (plan Appendix B, built in the gaps pass W11d).</summary>
public class CargoExpanderTests
{
    private static List<ShipModuleStep> Steps() =>
        RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.CargoExpander).OrderBy(x => x.Rank).ToList();

    [Test]
    public void CargoExpander_IsALowSlotPilotingPassiveLadder()
    {
        var steps = Steps();
        steps.Select(x => x.Rank).Should().Equal(0, 5, 10, 15, 20, 25, 30, 35, 40, 45);
        steps.Should().OnlyContain(x => x.Row == ShipModulePowerType.Low && x.Skill == SkillType.Piloting &&
                                        x.Activation == ShipModuleActivation.Invalid && !x.Capital);
        steps.Select(x => x.Tag).Should().Equal(steps.Select(x => $"sm_cargo_{x.Rank:00}"));
        ((int)ShipModuleFamily.CargoExpander).Should().Be(46);
        steps.Should().OnlyContain(x => x.Texture == "iit_smlogconfig", "it reuses the retired Logistics Configuration art");
    }

    [Test]
    public void EachStep_AddsItsShareOfTheHullsCargo_AndCostsFivePercentSpeed()
    {
        foreach (var step in Steps())
        {
            step.CargoPercent.Should().Be(ShipCargoRules.GetExpanderPercent(step.Rank));
            step.SpeedPercent.Should().Be(-5);
            step.Description.Should().Contain($"+{step.CargoPercent}% cargo bay");
        }

        Steps().First().CargoPercent.Should().Be(20);
        Steps().Last().CargoPercent.Should().Be(35);
    }

    [Test]
    public void Recipes_FollowTheHighestEarlierRecipe_SoNoValueMoves()
    {
        var recipes = Steps().Select(x => (int)x.RecipeType).ToList();
        recipes.Should().Equal(Enumerable.Range(7733, 10));
        recipes.First().Should().BeGreaterThan(DroidIndustryTuning.LastRecipeType);
        Enum.GetValues<RecipeType>().Select(x => (int)x).Where(x => recipes.Contains(x)).Should().HaveCount(10, "no other recipe shares these values");
        Steps().Should().OnlyContain(x => x.RecipeLevel == Math.Max(2, x.Rank));
    }

    [Test]
    public void Expanders_Stack_AndAHullWithNoCargoStaysEmpty()
    {
        ShipCargoRules.GetCapacity(480, Array.Empty<string>()).Should().Be(480);
        ShipCargoRules.GetCapacity(480, new[] { "sm_cargo_00" }).Should().Be(480 + 96);
        ShipCargoRules.GetCapacity(480, new[] { "sm_cargo_45", "sm_cargo_45" }).Should().Be(480 + 168 + 168);
        ShipCargoRules.GetCapacity(480, new[] { "sm_thrust_10", "sm_laser_00", null }).Should().Be(480, "other modules add no cargo");
        ShipCargoRules.GetCapacity(0, new[] { "sm_cargo_45" }).Should().Be(0);
    }

    [Test]
    public void AnExpander_CannotComeOffWhileTheBayWouldOverflow()
    {
        var fitted = new[] { "sm_cargo_45", "sm_laser_00" };
        ShipCargoRules.CanRemoveModule(480, fitted, "sm_cargo_45", 480).Should().BeTrue();
        ShipCargoRules.CanRemoveModule(480, fitted, "sm_cargo_45", 481).Should().BeFalse();
        ShipCargoRules.CanRemoveModule(480, fitted, "sm_laser_00", 648).Should().BeTrue("other modules never hold cargo");
    }

    [Test]
    public void CargoEnhancement_AddsItsPercentOfTheHullsCargo()
    {
        var bonuses = new ShipBonuses();
        bonuses.Add(59, 3);
        bonuses.Add(59, 2);
        bonuses.Cargo.Should().Be(5, "Starship Bonus subtype 59 is Cargo, and two enhancements stack");

        var status = new ShipStatus { Size = ShipSize.Small, DeedBonuses = bonuses };
        ShipCargoRules.GetCapacity(480, status).Should().Be(480 + 24, "+5% of a 480 bay");
        ShipCargoRules.GetCapacity(0, status).Should().Be(0, "a hull with no cargo gains none");
        ShipCargoRules.GetCapacity(480, new ShipStatus()).Should().Be(480, "an older ship with no deed bonuses");

        // Taking off an expander counts the enhancement on both sides of the check.
        var fitted = new[] { ("sm_cargo_45", 0) };
        var withoutExpander = 480 + 24;
        ShipCargoRules.CanRemoveModule(480, fitted, ("sm_cargo_45", 0), withoutExpander, 5).Should().BeTrue();
        ShipCargoRules.CanRemoveModule(480, fitted, ("sm_cargo_45", 0), withoutExpander + 1, 5).Should().BeFalse();

        ReadServerSource("Service", "Craft.cs").Should().Contain(
            "case EnhancementSubType.StarshipCargo: // Starship Cargo\n                    return ItemPropertyCustom(ItemPropertyType.StarshipBonus, 59, amount);");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs")
            .Should().Contain("ShipCargoRules.GetDeedCargoPercent(current.Status)))", "the removal check counts the deed's Cargo enhancements");
    }

    [Test]
    public void ModuleBonus_WidensAnExpandersShare()
    {
        var plain = ShipCargoRules.GetCapacity(480, new[] { "sm_cargo_45" });
        var bonused = ShipCargoRules.GetCapacity(480, new[] { ("sm_cargo_45", 10) });
        bonused.Should().BeGreaterThan(plain, "+10 Module Bonus widens the expander's share 10%");

        // Taking off the bonused expander checks the bay without its larger share.
        var fitted = new[] { ("sm_cargo_45", 10), ("sm_cargo_45", 0) };
        var withoutBonused = ShipCargoRules.GetCapacity(480, new[] { ("sm_cargo_45", 0) });
        ShipCargoRules.CanRemoveModule(480, fitted, ("sm_cargo_45", 10), withoutBonused).Should().BeTrue();
        ShipCargoRules.CanRemoveModule(480, fitted, ("sm_cargo_45", 10), withoutBonused + 1).Should().BeFalse();

        var status = new ShipStatus { Size = ShipSize.Small, SlotTags = new Dictionary<int, ShipSlotTag> { [11] = ShipSlotTag.Helm } };
        status.LowPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "sm_cargo_45", ModuleBonus = 10 };
        ShipCargoRules.GetCapacity(480, status).Should().Be(bonused, "a fitted expander's bonus reaches the bay");
    }

    [Test]
    public void OnlyHelmSlotExpanders_AddCargo()
    {
        // Low slots 11-13 on a hull with three Low slots, the last a station (Engineer) slot.
        var status = new ShipStatus
        {
            Size = ShipSize.Medium,
            SlotTags = new Dictionary<int, ShipSlotTag> { [11] = ShipSlotTag.Helm, [12] = ShipSlotTag.Helm, [13] = ShipSlotTag.Engineer },
        };
        status.LowPowerModules[1] = new ShipStatus.ShipStatusModule { ItemTag = "sm_cargo_00" };
        status.LowPowerModules[3] = new ShipStatus.ShipStatusModule { ItemTag = "sm_cargo_45" };

        ShipCargoRules.GetHelmModuleTags(status).Should().Equal("sm_cargo_00");
        ShipCargoRules.GetCapacity(480, status).Should().Be(480 + 96, "an expander in an empty station slot adds nothing");
        ShipCargoRules.IsCargoExpander("sm_cargo_45").Should().BeTrue();
        ShipCargoRules.IsCargoExpander("sm_laser_00").Should().BeFalse();
        RevampModuleCatalog.Steps.Where(x => x.Family is ShipModuleFamily.CargoExpander or ShipModuleFamily.SmugglingCompartment)
            .Should().OnlyContain(x => ShipCargoRules.IsHelmOnlyModule(x.Tag));
        ShipCargoRules.IsHelmOnlyModule("sm_laser_00").Should().BeFalse();
        ReadServerSource("Service", "TradeOrbitRisk.cs").Should().Contain("return ShipCargoRules.GetHelmModules(ship?.Status);",
            "a compartment in an empty station slot gives no customs cut, and a Helm one counts its Module Bonus");

        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs")
            .Should().Contain("if (ShipCargoRules.IsHelmOnlyModule(itemTag))")
            .And.Contain("SendMessageToPC(Player, \"Cargo Expanders and Smuggling Compartments fit Helm slots only.\");",
                "a station slot refuses an expander")
            .And.Contain("ShipCargoRules.CanRemoveModule(hullCargo, ShipCargoRules.GetHelmModules(current.Status).ToList(),")
            .And.Contain("(fitted.ItemTag, ShipTraitRules.ModuleBonus(fitted)), Trade.GetUsedVolume(current),",
                "the removal check counts the expander's Module Bonus")
            .And.Contain("ShipCargoRules.GetDeedCargoPercent(current.Status)))",
                "the removal check counts the Cargo enhancement");
    }

    [Test]
    public void TheCargoBay_HudAndRemoval_ReadTheExpanders()
    {
        ReadServerSource("Service", "Trade.cs").Should().Contain("return ShipCargoRules.GetCapacity(hullCargo, ship?.Status);");
        ReadServerSource("Service", "SpaceHud.cs").Should().Contain("ShipCargoRules.GetCapacity(ship.Hull.Cargo, status)");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs")
            .Should().Contain("ShipCargoRules.CanRemoveModule(hullCargo,");
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
