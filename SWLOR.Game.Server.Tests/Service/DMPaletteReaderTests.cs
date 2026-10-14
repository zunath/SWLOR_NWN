using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Tests.Service;

public class DMPaletteReaderTests
{
    private const string Palette = """
        {"__data_type":"ITP ","MAIN":{"type":"list","value":[
          {"__struct_id":0,"NAME":{"type":"cexostring","value":"World Creatures"},"LIST":{"type":"list","value":[
            {"__struct_id":0,"ID":{"type":"byte","value":156},"STRREF":{"type":"dword","value":16860238},"LIST":{"type":"list","value":[
              {"__struct_id":0,"CR":{"type":"float","value":4.0},"FACTION":{"type":"cexostring","value":"Hostile"},"NAME":{"type":"cexostring","value":"Rancor"},"RESREF":{"type":"resref","value":"dath_rancor"}}
            ]}},
            {"__struct_id":0,"ID":{"type":"byte","value":157},"STRREF":{"type":"dword","value":16778365}}
          ]}},
          {"__struct_id":0,"STRREF":{"type":"dword","value":6731},"NAME":{"type":"cexostring","value":"NPCs"},"LIST":{"type":"list","value":[
            {"__struct_id":0,"ID":{"type":"byte","value":44},"STRREF":{"type":"dword","value":35},"LIST":{"type":"list","value":[
              {"__struct_id":0,"RESREF":{"type":"resref","value":"guard"},"STRREF":{"type":"dword","value":500}}
            ]}},
            {"__struct_id":0,"ID":{"type":"byte","value":45},"STRREF":{"type":"dword","value":36},"LIST":{"type":"list","value":[
              {"__struct_id":0,"RESREF":{"type":"resref","value":"merchant"},"NAME":{"type":"cexostring","value":"Merchant"}}
            ]}}
          ]}}
        ]}}
        """;

    private static readonly Dictionary<int, string> Tlk = new()
    {
        [16860238] = "Dathomir",
        [16778365] = "Dantooine",
        [35] = "Humans",
        [36] = "Humans2",
        [500] = "Town Guard"
    };

    private static DMPalette Read() =>
        DMPaletteReader.Read(Palette, DMBlueprintCategory.Creatures, DMBlueprintSource.Custom,
            strRef => Tlk.TryGetValue(strRef, out var text) ? text : $"#{strRef}");

    [Test]
    public void Read_BuildsCategoryPathsInTreeOrderAndDropsEmptyCategories()
    {
        Read().CategoryPaths.Should().Equal(
            "World Creatures",
            "World Creatures / Dathomir",
            "NPCs",
            "NPCs / Humans",
            "NPCs / Humans2");
    }

    [Test]
    public void Read_ListsBlueprintsWithNamesCategoriesAndCreatureDetails()
    {
        var blueprints = Read().Blueprints;

        blueprints.Select(blueprint => blueprint.Resref).Should().Equal("dath_rancor", "guard", "merchant");
        blueprints[0].Name.Should().Be("Rancor");
        blueprints[0].CategoryPath.Should().Be("World Creatures / Dathomir");
        blueprints[0].ChallengeRating.Should().Be(4f);
        blueprints[0].Faction.Should().Be("Hostile");
        blueprints[1].Name.Should().Be("Town Guard", "a blueprint without NAME uses its TLK string");
        blueprints[2].ChallengeRating.Should().BeNull();
        blueprints.Should().OnlyContain(blueprint => blueprint.Source == DMBlueprintSource.Custom &&
                                                    blueprint.Category == DMBlueprintCategory.Creatures);
    }

    [Test]
    public void IsInCategory_MatchesTheCategoryAndItsChildrenOnly()
    {
        var blueprints = Read().Blueprints;
        var guard = blueprints.Single(blueprint => blueprint.Resref == "guard");

        guard.IsInCategory(string.Empty).Should().BeTrue();
        guard.IsInCategory("NPCs").Should().BeTrue();
        guard.IsInCategory("NPCs / Humans").Should().BeTrue();
        guard.IsInCategory("NPCs / Human").Should().BeFalse();
        blueprints.Single(blueprint => blueprint.Resref == "merchant").IsInCategory("NPCs / Humans").Should().BeFalse();
    }

    [Test]
    public void Read_SkipsMalformedValuesWithoutDroppingThePalette()
    {
        const string malformed = """
            {"MAIN":{"type":"list","value":[
              {"NAME":{"type":"cexostring","value":"NPCs"},"LIST":{"type":"list","value":[
                {"RESREF":{"type":"resref","value":7},"NAME":{"type":"cexostring","value":"Numeric resref"}},
                {"RESREF":{"type":"resref","value":"guard"},"NAME":{"type":"cexostring","value":"Guard"},"FACTION":{"type":"cexostring","value":3}}
              ]}}
            ]}}
            """;

        var palette = DMPaletteReader.Read(malformed, DMBlueprintCategory.Creatures, DMBlueprintSource.Custom, _ => string.Empty);

        palette.Blueprints.Select(blueprint => blueprint.Resref).Should().Equal("guard");
        palette.Blueprints[0].Faction.Should().BeEmpty();
    }

    [TestCase("creaturepalcus", DMBlueprintCategory.Creatures)]
    [TestCase("itempalcus", DMBlueprintCategory.Items)]
    [TestCase("placeablepalcus", DMBlueprintCategory.Placeables)]
    [TestCase("storepalcus", DMBlueprintCategory.Stores)]
    [TestCase("waypointpalcus", DMBlueprintCategory.Waypoints)]
    [TestCase("triggerpalcus", DMBlueprintCategory.Triggers)]
    [TestCase("encounterpalcus", DMBlueprintCategory.Encounters)]
    [TestCase("doorpalcus", DMBlueprintCategory.Doors)]
    public void Read_ModulePaletteListsEveryBlueprintUnderAListedCategory(string paletteName, DMBlueprintCategory category)
    {
        var json = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "Module", "itp", paletteName + ".itp.json"));
        var palette = DMPaletteReader.Read(json, category, DMBlueprintSource.Custom, strRef => $"#{strRef}");

        palette.Blueprints.Should().HaveCount(CountLeaves(json));
        palette.Blueprints.Should().OnlyContain(blueprint => palette.CategoryPaths.Contains(blueprint.CategoryPath));
        palette.CategoryPaths.Should().OnlyContain(path => palette.Blueprints.Any(blueprint => blueprint.IsInCategory(path)));
    }

    private static int CountLeaves(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Count(document.RootElement.GetProperty("MAIN").GetProperty("value"));

        static int Count(JsonElement nodes) => nodes.EnumerateArray().Sum(node =>
            node.TryGetProperty("RESREF", out _)
                ? 1
                : node.TryGetProperty("LIST", out var list) ? Count(list.GetProperty("value")) : 0);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
