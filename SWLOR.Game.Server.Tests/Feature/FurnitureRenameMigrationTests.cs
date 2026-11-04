using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class FurnitureRenameMigrationTests
{
    // Values the renamed members held under their old names; a rename must never change a stored value.
    private static readonly Dictionary<string, (RecipeType Renamed, int Recipe, int Structure)> FormerValues = new()
    {
        ["AshmarkDisplayPlinth"] = (RecipeType.AshmarkAltar, 1342, 339),
        ["GuttermarkDisplayPlinth"] = (RecipeType.GuttermarkEasel, 1352, 349),
        ["TrailmarkDisplayPlinth"] = (RecipeType.TrailmarkWaystone, 1362, 359),
        ["StonewakeDisplayPlinth"] = (RecipeType.StonewakeStoneAltar, 1372, 369),
        ["AegislineDisplayPlinth"] = (RecipeType.AegislineSwordPedestal, 1382, 379),
        ["VeilcarvedDisplayPlinth"] = (RecipeType.VeilcarvedRunePillar, 1392, 389),
        ["SurgewakeDisplayPlinth"] = (RecipeType.SurgewakeHoloProjector, 1402, 399),
        ["ResonantDisplayPlinth"] = (RecipeType.ResonantCrystal, 1412, 409),
        ["FieldlineDisplayPlinth"] = (RecipeType.FieldlineDisplayCase, 1422, 419),
        ["CommandlineDisplayPlinth"] = (RecipeType.CommandlineBust, 1432, 429),
    };

    [Test]
    public void Renames_KeepEveryStoredValueAndRetireTheOldNames()
    {
        foreach (var (oldName, (renamed, recipe, structure)) in FormerValues)
        {
            ((int)renamed).Should().Be(recipe, oldName);
            ((int)Enum.Parse<StructureType>(renamed.ToString())).Should().Be(structure, oldName);
            Enum.TryParse<RecipeType>(oldName, out _).Should().BeFalse(oldName);
            Enum.TryParse<StructureType>(oldName, out _).Should().BeFalse(oldName);
        }
    }

    [Test]
    public void PlacedStructures_LoseOnlyTheirFormerDefaultName()
    {
        WorldProperty Structure(StructureType type, string name) => new()
        {
            PropertyType = PropertyType.Structure,
            StructureType = type,
            CustomName = name,
        };

        var placed = Structure(StructureType.VeilcarvedRunePillar, "Veilcarved Display Plinth");
        FurnitureRenameMigration.RenameStructure(placed).Should().BeTrue();
        placed.CustomName.Should().Be("Veilcarved Rune Pillar");
        FurnitureRenameMigration.RenameStructure(placed).Should().BeFalse();

        var custom = Structure(StructureType.VeilcarvedRunePillar, "Trophy of the Hunt");
        FurnitureRenameMigration.RenameStructure(custom).Should().BeFalse();
        custom.CustomName.Should().Be("Trophy of the Hunt");

        var other = Structure(StructureType.VeilcarvedTrophyStand, "Veilcarved Display Plinth");
        FurnitureRenameMigration.RenameStructure(other).Should().BeFalse();
    }

    [Test]
    public void ItemText_RenamesOnlyTheRenamedStructuresAndTheirBlueprints()
    {
        FurnitureRenameMigration.GetRenamedItemText("structure_0429", "Commandline Display Plinth").Should().Be("Commandline Bust");
        FurnitureRenameMigration.GetRenamedItemText("bpstructure0429", "Blueprint: Commandline Display Plinth")
            .Should().Be("Blueprint: Commandline Bust");
        FurnitureRenameMigration.GetRenamedItemText("structure_0423", "Commandline Display Plinth").Should().Be("Commandline Display Plinth");
        FurnitureRenameMigration.GetRenamedItemText("structure_0429", "My Bust").Should().Be("My Bust");
        FurnitureRenameMigration.GetRenamedItemText("structure_0339", "Ashmark Display Plinth Replica")
            .Should().Be("Ashmark Display Plinth Replica", "a custom name that quotes the old title is kept");
        FurnitureRenameMigration.GetRenamedItemText("bpstructure0339", "A rare schematic for crafting Ashmark Display Plinth.")
            .Should().Be("A rare schematic for crafting Ashmark Altar.");
    }
}
