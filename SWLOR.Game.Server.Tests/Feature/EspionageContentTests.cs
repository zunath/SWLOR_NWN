using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

public class EspionageContentTests
{
    private static readonly string[] BasicSupplies = { "trace_fuse_1", "slt_ratchet", "stt_sampler" };

    [Test]
    public void EveryTrapAndPoisonBlueprint_HasTheActivationRequiredByItsHandler()
    {
        var definitions = new TrapKitItemDefinition().BuildItems()
            .Concat(new VenomCoatingItemDefinition().BuildItems()).ToDictionary(x => x.Key, x => x.Value);
        Assert.That(definitions, Has.Count.EqualTo(15));
        foreach (var (tag, definition) in definitions)
        {
            var item = Read("uti", tag);
            Assert.That(Text(item, "Tag"), Is.EqualTo(tag));
            var expectedSpell = tag.StartsWith("trap_kit_") ? CastSpell.UNIQUE_POWER_SELF_ONLY : CastSpell.UNIQUE_POWER;
            Assert.That(definition.ActivationSpell, Is.EqualTo(expectedSpell), "Saved items must receive the same activation on login/acquisition.");
            var activations = List(item, "PropertiesList").Where(p => Number(p, "PropertyName") == 15).ToArray();
            Assert.That(activations, Has.Length.EqualTo(1), tag);
            Assert.That(Number(activations[0], "Subtype"), Is.EqualTo((int)expectedSpell), tag);
            Assert.That(Number(activations[0], "CostTable"), Is.EqualTo(3), tag);
            Assert.That(Number(activations[0], "CostValue"), Is.EqualTo((int)CastSpellNumberUses.UNLIMITED_USE),
                "The handler consumes exactly one item only after success, rather than the engine consuming it first.");
            Assert.That(item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString(), Does.Contain("Activate Item"));
            Assert.That(definition.ValidateAction, Is.Not.Null);
            Assert.That(definition.ApplyAction, Is.Not.Null);
        }
    }

    [Test]
    public void ActivationMetadata_DoesNotChangeUnconfiguredItemsAndAppliesToEveryBuilderTag()
    {
        var builder = new ItemBuilder();
        var items = builder.Create("plain").Create("first", "second")
            .ActivationSpell(CastSpell.UNIQUE_POWER_SELF_ONLY).Build();
        Assert.That(items["plain"].ActivationSpell, Is.Null);
        Assert.That(items["first"].ActivationSpell, Is.EqualTo(CastSpell.UNIQUE_POWER_SELF_ONLY));
        Assert.That(items["second"].ActivationSpell, Is.EqualTo(CastSpell.UNIQUE_POWER_SELF_ONLY));
    }

    [Test]
    public void VelesWorkbench_IsPlacedUsableAndMarkedOnTheMap()
    {
        var area = Read("git", "veles_shops");
        var benches = List(area, "Placeable List").Where(p => Text(p, "TemplateResRef") == "espionage_bench").ToArray();
        Assert.That(benches, Has.Length.EqualTo(1));
        foreach (var bench in new[] { benches[0], Read("utp", "espionage_bench") })
        {
            Assert.That(Number(bench, "Useable"), Is.EqualTo(1));
            Assert.That(Number(bench, "Static"), Is.Zero);
            Assert.That(Text(bench, "OnUsed"), Is.EqualTo("craft_on_used"));
            var skill = List(bench, "VarTable").Single(v => Text(v, "Name") == "CRAFTING_SKILL_TYPE_ID");
            Assert.That(Number(skill, "Value"), Is.EqualTo((int)SkillType.Espionage));
        }

        var marker = List(area, "WaypointList").Single(w => Text(w, "Tag") == "WP_ESPIONAGE_BENCH");
        Assert.That(Number(marker, "HasMapNote"), Is.EqualTo(1));
        Assert.That(Number(marker, "MapNoteEnabled"), Is.EqualTo(1));
        foreach (var axis in new[] { "X", "Y", "Z" })
            Assert.That(marker.GetProperty(axis + "Position").GetProperty("value").GetDouble(),
                Is.EqualTo(benches[0].GetProperty(axis).GetProperty("value").GetDouble()).Within(0.001));
    }

    [Test]
    public void VelesVendor_SellsOnlyBasicSlicingAssistanceInBothBlueprintAndPlacedStore()
    {
        var placed = List(Read("git", "veles_genstore"), "StoreList")
            .Single(s => Text(s, "Tag") == "VELES_GENERAL_STORE_MERCHANT");
        var stores = new[] { Read("utm", "veles_gen_merch"), placed };
        foreach (var store in stores)
        {
            var items = List(store, "StoreList").SelectMany(panel => List(panel, "ItemList")).ToArray();
            var supplies = items.Where(i =>
            {
                var resref = Text(i, i.TryGetProperty("InventoryRes", out _) ? "InventoryRes" : "TemplateResRef");
                return resref.StartsWith("slt_") || resref.StartsWith("stt_") || resref.StartsWith("trace_fuse_");
            }).ToArray();
            Assert.That(supplies.Select(i => Text(i, i.TryGetProperty("InventoryRes", out _) ? "InventoryRes" : "TemplateResRef")),
                Is.EquivalentTo(BasicSupplies));
            foreach (var supply in supplies)
                Assert.That(Number(supply, "Infinite"), Is.EqualTo(1));
        }

        foreach (var resref in BasicSupplies)
        {
            var blueprint = Read("uti", resref);
            var item = List(placed, "StoreList").SelectMany(panel => List(panel, "ItemList"))
                .Single(i => Text(i, "TemplateResRef") == resref);
            Assert.That(Number(item, "Cost"), Is.EqualTo(Number(blueprint, "Cost")));
            Assert.That(Number(item, "AddCost"), Is.EqualTo(Number(blueprint, "AddCost")));
            Assert.That(Number(item, "StackSize"), Is.EqualTo(1));
            Assert.That(Number(List(item, "VarTable").Single(v => Text(v, "Name") == "SLICING_TOOL_TIER"), "Value"), Is.EqualTo(1));
        }
    }

    private static JsonElement Read(string folder, string resref)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "Module", folder, $"{resref}.{folder}.json")));
        return document.RootElement.Clone();
    }

    private static JsonElement.ArrayEnumerator List(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").EnumerateArray();
    private static string Text(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetString()!;
    private static int Number(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetInt32();
}
