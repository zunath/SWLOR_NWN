using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class FurnitureRenameMigrationTests
{
    // Values the renamed members held under their old names; a rename must never change a stored value.
    private static readonly Dictionary<string, (int Recipe, int Structure)> FormerValues = new()
    {
        ["AshmarkDisplayPlinth"] = (1342, 339),
        ["GuttermarkDisplayPlinth"] = (1352, 349),
        ["TrailmarkDisplayPlinth"] = (1362, 359),
        ["StonewakeDisplayPlinth"] = (1372, 369),
        ["AegislineDisplayPlinth"] = (1382, 379),
        ["VeilcarvedDisplayPlinth"] = (1392, 389),
        ["SurgewakeDisplayPlinth"] = (1402, 399),
        ["ResonantDisplayPlinth"] = (1412, 409),
        ["FieldlineDisplayPlinth"] = (1422, 419),
        ["CommandlineDisplayPlinth"] = (1432, 429),
    };

    [Test]
    public void Renames_KeepEveryStoredValueAndRetireTheOldNames()
    {
        FurnitureRenameMigration.RecipeRenames.Keys.Should().BeEquivalentTo(FormerValues.Keys);
        foreach (var (oldName, (recipe, structure)) in FormerValues)
        {
            var renamed = FurnitureRenameMigration.RecipeRenames[oldName];
            ((int)renamed).Should().Be(recipe, oldName);
            ((int)Enum.Parse<StructureType>(renamed.ToString())).Should().Be(structure, oldName);
            Enum.TryParse<RecipeType>(oldName, out _).Should().BeFalse(oldName);
            Enum.TryParse<StructureType>(oldName, out _).Should().BeFalse(oldName);
        }
    }

    [Test]
    public void StalePlayerJson_DeserializesAfterMigrationWithRecipeKnowledgePreserved()
    {
        var unlocked = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var crafted = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var player = JObject.Parse(
            "{\"Id\":\"furniture-player\"," +
            "\"UnlockedRecipes\":{\"VeilcarvedDisplayPlinth\":\"2026-10-01T00:00:00Z\",\"VeilcarvedTrophyStand\":\"2026-09-01T00:00:00Z\"," +
            "\"CommandlineBust\":\"2026-08-01T00:00:00Z\",\"CommandlineDisplayPlinth\":\"2026-10-03T00:00:00Z\"}," +
            "\"CraftedRecipes\":{\"VeilcarvedDisplayPlinth\":\"2026-10-02T00:00:00Z\"}," +
            "\"UnknownField\":{\"value\":42}}");
        var unknown = player["UnknownField"]!.DeepClone();

        void AssertMigrated(Player migrated)
        {
            migrated.UnlockedRecipes[RecipeType.VeilcarvedRunePillar].Should().Be(unlocked);
            migrated.UnlockedRecipes.Should().ContainKey(RecipeType.VeilcarvedTrophyStand);
            migrated.UnlockedRecipes[RecipeType.CommandlineBust].Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                "an entry already under the new name is kept");
            migrated.CraftedRecipes.Should().ContainSingle().Which.Should()
                .Be(new KeyValuePair<RecipeType, DateTime>(RecipeType.VeilcarvedRunePillar, crafted));
        }

        // A record that skipped server migration 29 (e.g. restored afterwards) still loads, with the former names mapped.
        AssertMigrated(JsonConvert.DeserializeObject<Player>(player.ToString())!);

        _29_RenameFurnitureRecipes.MigratePlayer(player, out var saved).Should().BeTrue();
        AssertMigrated(saved);
        _29_RenameFurnitureRecipes.MigratePlayer(player, out _).Should().BeFalse();
        JToken.DeepEquals(player["UnknownField"], unknown).Should().BeTrue();
        player[nameof(Player.UnlockedRecipes)]!["VeilcarvedDisplayPlinth"].Should().BeNull();

        var reserialized = JObject.Parse(JsonConvert.SerializeObject(saved));
        reserialized[nameof(Player.UnlockedRecipes)]![nameof(RecipeType.VeilcarvedRunePillar)].Should().NotBeNull();
        AssertMigrated(JsonConvert.DeserializeObject<Player>(reserialized.ToString())!);
    }

    [Test]
    public void Migration_RunsOnRawJsonBeforeTypedMigrations()
    {
        var migration = new _29_RenameFurnitureRecipes();
        migration.ExecutionType.Should().Be(MigrationExecutionType.PostDatabaseLoad);
        migration.Version.Should().Be(29);
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
    }
}
