using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipEquipmentRecipeTests
{
    [Test]
    public void EveryAllowedCalibration_IsCraftableWithItsExactMaterialsAndOneEligibleQualitySlot()
    {
        var recipes = new ShipEquipmentRecipes().BuildRecipes().Values.ToDictionary(x => x.Resref);
        var spec = ShipFittingTests.Specification(); var catalog = ShipFittingCatalog.Default;
        recipes.Should().HaveCount(269);
        foreach (var variant in catalog.Variants.Values)
        {
            var recipe = recipes[variant.ItemResref];
            var source = spec["recipes"]!.Single(x => (string)x["id"]! == variant.Design);
            recipe.IsActive.Should().BeTrue(); recipe.IsShipEquipment.Should().BeTrue();
            recipe.Level.Should().Be(variant.EngineeringRank);
            recipe.Quantity.Should().Be((int)source["output"]!);
            foreach (var (field, resref) in new[] { ("tilarium", "ref_tilarium"), ("currian", "ref_currian"), ("electronics", "elec_ruined"), ("recovered", "elec_recover"), ("precision", "prec_assembly") })
                recipe.Components.GetValueOrDefault(resref).Should().Be((int)source[field]! + (field == "recovered" && variant.Calibration != "Standard" ? 1 : 0));
            recipe.ShipQualityDimensions.Should().Be(catalog.Modules[variant.Design].QualityDimensions);
            recipe.EnhancementSlots.Should().Be(recipe.ShipQualityDimensions == ShipQualityDimension.None ? 0 : 1);
            recipe.Requirements.Single().RequirementText.Should().StartWith("Requires Ship Manufacturing ");
            var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(ShipFittingTests.Root(), "Module", "uti", variant.ItemResref + ".uti.json")));
            ((string)blueprint["Tag"]!["value"]!).Should().Be(catalog.Modules[variant.Design].ItemTag);
            var variables = blueprint["VarTable"]!["value"]!.ToDictionary(x => (string)x["Name"]!["value"]!, x => x["Value"]!["value"]!);
            ((string)variables["SHIP_CALIBRATION"]).Should().Be(variant.Calibration);
            ((int)variables["SHIP_CONDITION"]).Should().Be(100);
            ((int)variables["NPC_RESALE_LIMIT"]).Should().Be((int)Math.Floor(Math.Min(catalog.Modules[variant.Design].ReferenceValue * .25, (double)source["reference_cost"]! * .75)));
        }
    }

    [Test]
    public void OldTierRecipes_AreInactiveAndCanonicalModulesHaveNoOldPerkGates()
    {
        foreach (var definition in new IRecipeListDefinition[] { new ModuleRecipes(), new CapitalModuleRecipes(), new StarshipRecipes(), new CapitalConstructionRecipes(), new StarshipAmmoRecipes() })
            definition.BuildRecipes().Values.Should().OnlyContain(x => !x.IsActive);
        var modules = new ShipEquipmentModuleDefinition().BuildShipModules(); modules.Should().HaveCount(52);
        foreach (var profile in ShipFittingCatalog.Default.Modules.Values)
        {
            var module = modules[profile.ItemTag]; module.FittingProfile.Should().Be(profile);
            module.RequiredPerks.Should().BeEmpty(); module.Texture.Should().NotBeNullOrWhiteSpace();
        }
        ShipFittingCatalog.Default.DesignByItemTag("hull_plating").Should().BeNull("raw material tags must never identify fitted ship equipment");
    }

    [Test]
    public void AllSixRefinementProperties_HaveMatchingCompiledLabelsAndBoundedInputs()
    {
        var tlk = TlkReader.Read(File.ReadAllBytes(Path.Combine(ShipFittingTests.Root(), "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk")));
        var rows = File.ReadLines(Path.Combine(ShipFittingTests.Root(), "SWLOR_Haks", "sw_2da", "iprp_shiptune.2da"))
            .Select(x => x.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries)).Where(x => x.Length >= 3 && int.TryParse(x[0], out _)).ToArray();
        rows.Should().HaveCount(6);
        foreach (var row in rows)
        {
            var subtype = int.Parse(row[0]); var dimension = ShipRefinement.Dimension(subtype);
            ShipRefinement.Subtype(dimension).Should().Be(subtype);
            tlk.Entries[int.Parse(row[1]) - 16777216].Text.Should().Contain("Refinement");
            if (dimension != ShipQualityDimension.ActivationCost) ShipRefinement.Validate(dimension, dimension, 100).Should().BeNull();
            ShipRefinement.Validate(dimension, dimension, 101).Should().NotBeNull();
        }
    }
}
