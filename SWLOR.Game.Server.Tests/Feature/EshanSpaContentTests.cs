using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanSpaContentTests
{
    [Test]
    public void SpaCivilians_PlacedInstancesKeepBlueprintAppearanceAndDialogueWiring()
    {
        using var area = Read("Module/git/pw_sc_eshspa.git.json");
        var civilians = area.RootElement.GetProperty("Creature List").GetProperty("value")
            .EnumerateArray().Where(IsSpaCivilian).ToArray();
        civilians.Should().HaveCount(4);

        foreach (var placed in civilians)
        {
            var resref = Value(placed, "TemplateResRef").GetString()!;
            using var blueprint = Read($"Module/utc/{resref}.utc.json");
            foreach (var property in blueprint.RootElement.EnumerateObject())
            {
                if (property.Name is "__data_type" or "PaletteID" or "Comment" or "Equip_ItemList")
                    continue;

                JsonEquals(placed.GetProperty(property.Name), property.Value).Should().BeTrue(
                    $"{resref} must keep its blueprint {property.Name} when placed in the resort");
            }

            Value(placed, "ScriptDialogue").GetString().Should().Be("dialog_start");
            var conversation = Value(placed, "Conversation").GetString()!;
            using var graph = Read($"SWLOR.Game.Server/ConversationData/{conversation}.conversation.json");
            graph.RootElement.GetProperty("Id").GetString().Should().Be(conversation);
            graph.RootElement.GetProperty("Choices").EnumerateObject().Should().OnlyContain(
                choice => choice.Value.GetProperty("Actions").GetArrayLength() == 0,
                "spa greetings and optional lore must not advance supply quests");
        }
    }

    [Test]
    public void SpaCivilians_MaterializedWardrobesMatchTheirNpcOnlyItemBlueprints()
    {
        using var area = Read("Module/git/pw_sc_eshspa.git.json");
        foreach (var placed in area.RootElement.GetProperty("Creature List").GetProperty("value")
                     .EnumerateArray().Where(IsSpaCivilian))
        {
            var resref = Value(placed, "TemplateResRef").GetString()!;
            using var blueprint = Read($"Module/utc/{resref}.utc.json");
            var equipment = Value(blueprint.RootElement, "Equip_ItemList").EnumerateArray().Single();
            var itemResref = Value(equipment, "EquippedRes").GetString()!;
            using var item = Read($"Module/uti/{itemResref}.uti.json");
            var worn = Value(placed, "Equip_ItemList").EnumerateArray().Single();

            worn.GetProperty("__struct_id").GetInt32().Should().Be(2,
                "civilian outfits belong in the armor equipment slot");
            Value(worn, "Dropable").GetInt32().Should().Be(0);
            Value(item.RootElement, "PropertiesList").GetArrayLength().Should().Be(0);
            Value(item.RootElement, "LocalizedName").GetProperty("0").GetString()
                .Should().StartWith("[NPC]");
            Value(item.RootElement, "VarTable").EnumerateArray().Should().Contain(
                variable => Value(variable, "Name").GetString() == "NO_ECONOMY" &&
                            Value(variable, "Value").GetInt32() == 1);

            foreach (var property in item.RootElement.EnumerateObject())
            {
                if (property.Name is "__data_type" or "Comment")
                    continue;

                JsonEquals(worn.GetProperty(property.Name), property.Value).Should().BeTrue(
                    $"{resref} must wear the same {property.Name} authored on {itemResref}");
            }
        }
    }

    private static bool IsSpaCivilian(JsonElement creature) =>
        Value(creature, "TemplateResRef").GetString()!.StartsWith("es_spa_", StringComparison.Ordinal);

    private static JsonElement Value(JsonElement element, string property) =>
        element.GetProperty(property).GetProperty("value");

    private static bool JsonEquals(JsonElement actual, JsonElement expected) =>
        JsonElement.DeepEquals(actual, expected);

    private static JsonDocument Read(string relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the module and conversation sources must be available");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName, relativePath)));
    }
}
