using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class GuiLayoutComposerTests
{
    private const string MainView =
        """{"type":"group","children":[{"type":"col","children":[{"type":"row","children":[{"type":"label","value":"Name"},{"type":"group","id":"tab_content","children":[{"type":"spacer"}]}]}]}]}""";

    private const string AttributesTab =
        """{"type":"group","children":[{"type":"col","children":[{"type":"label","value":"Might","width":112.0}]}]}""";

    private const string ArmorTab =
        """{"type":"group","children":[{"type":"col","children":[{"type":"group","id":"palette","width":308.0,"children":[{"type":"spacer"}]}]}]}""";

    private const string MetalPalette =
        """{"type":"group","children":[{"type":"label","value":"Metal"}]}""";

    [Test]
    public void Compose_PlacesTheAssignedLayoutAsTheGroupsOnlyChild()
    {
        var placed = new HashSet<string>();

        var composed = GuiLayoutComposer.Compose(MainView, Slots(("tab_content", AttributesTab)), placed);

        var slot = FindGroup(JsonNode.Parse(composed)!, "tab_content");
        slot!["children"]!.AsArray().Should().ContainSingle();
        JsonNode.DeepEquals(slot["children"]![0], JsonNode.Parse(AttributesTab)).Should().BeTrue();
        placed.Should().BeEquivalentTo("tab_content");
    }

    [Test]
    public void Compose_FillsGroupsInsidePlacedLayouts()
    {
        // The Appearance Editor shape: main view -> armor tab -> color palette.
        var placed = new HashSet<string>();

        var composed = GuiLayoutComposer.Compose(
            MainView,
            Slots(("tab_content", ArmorTab), ("palette", MetalPalette)),
            placed);

        var palette = FindGroup(JsonNode.Parse(composed)!, "palette");
        JsonNode.DeepEquals(palette!["children"]![0], JsonNode.Parse(MetalPalette)).Should().BeTrue();
        palette["width"]!.GetValue<double>().Should().Be(308.0);
        placed.Should().BeEquivalentTo("tab_content", "palette");
    }

    [Test]
    public void Compose_LeavesGroupsWithoutAnAssignedLayoutUnchanged()
    {
        var placed = new HashSet<string>();

        var composed = GuiLayoutComposer.Compose(MainView, Slots(("no_such_element_id", AttributesTab)), placed);

        JsonNode.DeepEquals(JsonNode.Parse(composed), JsonNode.Parse(MainView)).Should().BeTrue();
        placed.Should().BeEmpty("a layout for a group that is not in the tree must not be reported as shown");
    }

    [Test]
    public void Compose_DoesNotPlaceLayoutsIntoNonGroupElements()
    {
        const string layout = """{"type":"group","children":[{"type":"button","id":"tab_content","label":"Go"}]}""";

        var composed = GuiLayoutComposer.Compose(layout, Slots(("tab_content", AttributesTab)));

        JsonNode.DeepEquals(JsonNode.Parse(composed), JsonNode.Parse(layout)).Should().BeTrue();
    }

    [Test]
    public void Compose_KeepsTheDeclaredContentWhenALayoutContainsItsOwnGroup()
    {
        const string selfContaining =
            """{"type":"group","children":[{"type":"group","id":"tab_content","children":[{"type":"spacer"}]}]}""";

        var composed = GuiLayoutComposer.Compose(MainView, Slots(("tab_content", selfContaining)));

        var outer = FindGroup(JsonNode.Parse(composed)!, "tab_content");
        var inner = FindGroup(outer!["children"]![0]!, "tab_content");
        inner!["children"]![0]!["type"]!.GetValue<string>().Should().Be("spacer");
    }

    [Test]
    public void Compose_PreservesNumbersAndWritesAsciiOnly()
    {
        const string tab = """{"type":"group","children":[{"type":"label","value":"Café +5","width":250.0}]}""";

        var composed = GuiLayoutComposer.Compose(MainView, Slots(("tab_content", tab)));

        composed.Should().Contain("250.0");
        composed.Should().MatchRegex(@"^[\x20-\x7E]*$", "JsonParse reads the text in the game's local encoding");
        var slot = FindGroup(JsonNode.Parse(composed)!, "tab_content");
        FindLabel(slot!["children"]![0]!)!["value"]!.GetValue<string>().Should().Be("Café +5");
    }

    [Test]
    public void Compose_HandlesLayoutsDeeperThanTheDefaultJsonDepth()
    {
        var deep = AttributesTab;
        for (var level = 0; level < 60; level++)
            deep = $$"""{"type":"col","children":[{{deep}}]}""";

        var composed = GuiLayoutComposer.Compose(MainView, Slots(("tab_content", deep)));

        composed.Should().Contain("Might");
    }

    private static Dictionary<string, string> Slots(params (string Id, string Layout)[] slots) =>
        slots.ToDictionary(slot => slot.Id, slot => slot.Layout);

    private static JsonNode? FindGroup(JsonNode node, string id) =>
        Find(node, element => element["type"]?.GetValue<string>() == "group" && element["id"]?.GetValue<string>() == id);

    private static JsonNode? FindLabel(JsonNode node) =>
        Find(node, element => element["type"]?.GetValue<string>() == "label");

    private static JsonNode? Find(JsonNode node, Func<JsonObject, bool> match)
    {
        if (node is JsonObject element && match(element))
            return element;

        var children = node is JsonObject obj ? obj["children"] as JsonArray : null;
        if (children == null)
            return null;

        foreach (var child in children)
        {
            var found = child == null ? null : Find(child, match);
            if (found != null)
                return found;
        }

        return null;
    }
}
