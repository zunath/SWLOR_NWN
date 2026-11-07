using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The Fermentation Vat of the Player City Revamp: its structure, items, recipe, records and menu, and the 15 enzyme items it makes.</summary>
public class FermentationVatContentTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return directory!.FullName;
        }
    }

    private static StructureAttribute Vat => StructureType.FermentationVat.GetAttribute<StructureType, StructureAttribute>();

    private static JsonDocument Read(string folder, string resref, string extension) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", folder, resref + extension)).TrimStart('﻿'));

    private static int Number(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetInt32();

    private static string Text(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetString()!;

    // The structure

    [Test]
    public void TheVatIsAnActiveBasicStructureForHousesOnly()
    {
        Vat.IsActive.Should().BeTrue();
        Vat.Name.Should().Be("Fermentation Vat");
        Vat.RestrictedPropertyTypes.Should().Be(PropertyType.House, "a vat stands in a house in a city, never outdoors or in an apartment");
        Vat.LayoutType.Should().Be(PropertyLayoutType.Invalid, "a vat has no interior");
        Vat.Category.Should().Be(StructureCategoryType.Structure);
        Vat.CanBeRetrieved.Should().BeTrue();
        Vat.ItemStorage.Should().Be(0);
        Vat.Description.Should().MatchRegex("^[\\x20-\\x7E]+$").And.Contain("house").And.Contain("hunted");
    }

    [Test]
    public void TheVatTakesItsIdsFromTheReservedRanges()
    {
        ((int)StructureType.FermentationVat).Should().Be(5105).And.BeInRange(5100, 5129);
        ((int)RecipeType.FermentationVat).Should().Be(8040).And.BeInRange(8040, 8079, "slice 5d's own range inside the City Revamp's 8000-8199");
        ScriptName.OnFermentationVatUsed.Length.Should().BeLessThanOrEqualTo(16);
        ScriptName.OnFermentationVatUsed.Should().MatchRegex("^[a-z_]+$");
    }

    [Test]
    public void TheVatUsesAUsablePlaceableWithoutAnInventory()
    {
        using var blueprint = Read("utp", Vat.Resref, ".utp.json");

        Number(blueprint, "Static").Should().Be(0, "a static placeable cannot be used");
        Number(blueprint, "HasInventory").Should().Be(0, "a placeable with an inventory opens it instead of the vat menu");
        Text(blueprint, "Tag").Should().Be(Vat.Resref);
        Text(blueprint, "TemplateResRef").Should().Be(Vat.Resref);
        Vat.Resref.Length.Should().BeLessThanOrEqualTo(16);
        blueprint.RootElement.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString().Should().Be("Fermentation Vat");
    }

    [Test]
    public void TheVatItemIsAStructureItemOfItsId_CraftedNotFlagged()
    {
        using var item = Read("uti", "structure_5105", ".uti.json");

        Text(item, "Tag").Should().Be("structure_5105");
        Text(item, "TemplateResRef").Should().Be("structure_5105");
        item.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Fermentation Vat");
        item.RootElement.GetProperty("Description").GetProperty("value").GetProperty("0").GetString().Should().MatchRegex("^[\\x20-\\x7E]+$").And.Contain("house");
        item.RootElement.TryGetProperty("VarTable", out _).Should().BeFalse("players craft it, so it is not an NPC-only item");

        // The same item every other structure uses, so placing it works the way it does for the others.
        using var corral = Read("uti", "structure_5104", ".uti.json");
        Number(item, "BaseItem").Should().Be(Number(corral, "BaseItem"));
        item.RootElement.GetProperty("PropertiesList").GetRawText().Should().Be(corral.RootElement.GetProperty("PropertiesList").GetRawText());
    }

    [Test]
    public void TheVatItemIsInTheItemPalette()
    {
        var palette = File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"));

        palette.Should().Contain("\"value\": \"structure_5105\"").And.Contain("\"value\": \"Fermentation Vat\"");
    }

    // The recipe

    [Test]
    public void TheVatRecipeIsTheCookpotsLevelAndBill_AFabricationStructureForAgricultureWork()
    {
        var recipes = new FermentationVatRecipes().BuildRecipes();
        var cookpot = new FurnitureRecipes().BuildRecipes()[RecipeType.Cookpot];

        recipes.Should().ContainSingle().Which.Key.Should().Be(RecipeType.FermentationVat);
        var vat = recipes[RecipeType.FermentationVat];
        vat.Skill.Should().Be(SkillType.Fabrication);
        vat.Category.Should().Be(RecipeCategoryType.Crafting);
        vat.Resref.Should().Be("structure_5105");
        vat.Quantity.Should().Be(1);
        vat.IsActive.Should().BeTrue();
        vat.Level.Should().Be(cookpot.Level).And.Be(25);
        vat.Components.Should().BeEquivalentTo(cookpot.Components);
        vat.Category.Should().Be(cookpot.Category);
    }

    [Test]
    public void TheVatRecipeBillIsMadeOfItemsTheModuleHas()
    {
        foreach (var resref in new FermentationVatRecipes().BuildRecipes()[RecipeType.FermentationVat].Components.Keys)
            File.Exists(Path.Combine(Root, "Module", "uti", resref + ".uti.json")).Should().BeTrue(resref);
    }

    [Test]
    public void TheCulturesAreNotRecipes_SoNoRecipeTypeIsTakenForThem()
    {
        // A culture is a 2 day job picked at the vat, not a minigame recipe: the only recipe type this slice takes is the vat's.
        Enum.GetValues<RecipeType>().Where(type => (int)type is >= 8040 and <= 8079).Should().Equal(RecipeType.FermentationVat);
    }

    // The enzyme items

    [Test]
    public void AllFifteenEnzymeItemsExist_NamedForTheirTypeAndColor_WithTheirColorProperty()
    {
        foreach (var (type, color) in EnzymeCultureRules.GetAll())
        {
            var resref = EnzymeCultureRules.GetResref(type, color);
            using var item = Read("uti", resref, ".uti.json");

            Text(item, "Tag").Should().Be(BeastMastery.EnzymeTag);
            item.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be($"{type} Enzyme - {color}");
            var colorProperties = item.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
                .Where(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)ItemPropertyType.EnzymeColor)
                .ToList();
            colorProperties.Should().ContainSingle(resref);
            colorProperties[0].GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be((int)color, resref);
        }
    }

    [Test]
    public void TheFifteenAreExactlyTheColorsTheHuntedLootTablesDrop()
    {
        var tables = new EnzymeLootTableDefinition().BuildLootTables();
        var dropped = tables.Values.SelectMany(table => table).Select(entry => entry.Resref).Distinct().OrderBy(resref => resref).ToList();
        var made = EnzymeCultureRules.GetAll().Select(entry => EnzymeCultureRules.GetResref(entry.Type, entry.Color)).OrderBy(resref => resref).ToList();

        dropped.Should().Equal(made);
        made.Should().HaveCount(15);
    }

    [Test]
    public void TheHuntedTablesStillRollEveryEnzymeWithTheSameWeights()
    {
        var tables = new EnzymeLootTableDefinition().BuildLootTables();

        tables["HYDROLASE_TYPE_1"].Select(entry => (entry.Resref, entry.Weight)).Should().Equal(
            ("hydrolase_blue", 100), ("hydrolase_orange", 90), ("hydrolase_red", 70), ("hydrolase_purple", 20), ("hydrolase_white", 5));
        tables["ISOMERASE_TYPE_1"].Select(entry => (entry.Resref, entry.Weight)).Should().Equal(
            ("isomerase_orange", 100), ("isomerase_blue", 90), ("isomerase_green", 70), ("isomerase_red", 20), ("isomerase_yellow", 5));
        tables["LYASE_TYPE_1"].Select(entry => (entry.Resref, entry.Weight)).Should().Equal(
            ("lyase_green", 100), ("lyase_blue", 90), ("lyase_orange", 70), ("lyase_red", 20), ("lyase_black", 5));
        tables.Values.SelectMany(table => table).Should().OnlyContain(entry => entry.OnSpawn != null, "each enzyme still rolls its stats when it drops");
    }

    [Test]
    public void ACulturedEnzymeIsMarkedAndNamedAsOne_SoItIsNeverTakenForAStarter()
    {
        FermentationVats.CulturedVariable.Should().Be("ENZYME_CULTURED");
        FermentationVats.CulturedSuffix.Should().Be(" (Cultured)").And.MatchRegex("^[\\x20-\\x7E]+$");
        EnzymeCultureRules.CanBeStarter("hydrolase_blue", isCultured: true, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse();
    }

    // The records

    [Test]
    public void TheRecordsAreKeyedApartFromTheirVatStructure()
    {
        var vat = new FermentationVat();
        var job = new FermentationJob();

        FermentationVat.KeyFor("abc").Should().Be("fermentation-vat:abc").And.NotBe("abc");
        FermentationJob.KeyFor("abc").Should().Be("fermentation-job:abc").And.NotBe("abc").And.NotBe(FermentationVat.KeyFor("abc"));
        vat.VatId.Should().BeEmpty();
        vat.Starters.Should().BeEmpty();
        vat.EntityType.Should().Be(nameof(FermentationVat));
        job.EntityType.Should().Be(nameof(FermentationJob));
        job.VatId.Should().BeEmpty();
        job.OwnerPlayerId.Should().BeEmpty();
        job.OwnerCdKey.Should().BeEmpty();
        job.StarterData.Should().BeEmpty();
    }

    // The menu

    [Test]
    public void TheMenuHasAMainPageAndTheThreePickerPages()
    {
        var menu = new FermentationVatDialog().Build();

        menu.Pages.Keys.Should().BeEquivalentTo("VAT_PAGE", "TYPE_PAGE", "COLOR_PAGE", "CROP_PAGE");
        menu.DefaultPageId.Should().Be("VAT_PAGE");
        menu.DataModel.Should().NotBeNull();
        menu.InitializationActions.Should().ContainSingle("the menu finds its vat from the placeable it was opened on");
    }
}
