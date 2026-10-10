using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats.Tga;

namespace SWLOR.Game.Server.Tests.Feature;

public class LootTableDefinitionTests
{
    private static readonly HashSet<string> EngineItemTemplateResrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "nw_it_gold001"
    };

    [Test]
    public void LootTableItems_ResolveToModuleItemTemplates()
    {
        var root = FindRepositoryRoot();
        var itemTemplates = ReadModuleItemTemplateResrefs(root);
        var failures = new List<string>();

        foreach (var definitionType in GetLootTableDefinitionTypes())
        {
            var definition = (ILootTableDefinition)Activator.CreateInstance(definitionType)!;
            var tables = definition.BuildLootTables();

            foreach (var (tableId, table) in tables)
            {
                foreach (var item in table)
                {
                    if (!EngineItemTemplateResrefs.Contains(item.Resref) &&
                        !itemTemplates.Contains(item.Resref))
                    {
                        failures.Add($"{definitionType.Name}/{tableId}: '{item.Resref}' has no Module/uti TemplateResRef.");
                    }
                }
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void SaberLoot_OnlyDropsSolidTrainingWeaponsWithoutBladeEmitters()
    {
        var root = FindRepositoryRoot();
        var models = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        var trainingWeapons = new HashSet<string>();

        foreach (var definitionType in GetLootTableDefinitionTypes())
        {
            var definition = (ILootTableDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (tableId, table) in definition.BuildLootTables())
            {
                foreach (var item in table)
                {
                    if (EngineItemTemplateResrefs.Contains(item.Resref))
                        continue;

                    var path = Path.Combine(root.FullName, "Module", "uti", $"{item.Resref}.uti.json");
                    using var document = JsonDocument.Parse(File.ReadAllText(path));
                    var blueprint = document.RootElement;
                    var baseItem = (BaseItem)blueprint.GetProperty("BaseItem").GetProperty("value").GetInt32();
                    if (baseItem is not (BaseItem.Lightsaber or BaseItem.Saberstaff))
                        continue;

                    var source = $"{definitionType.Name}/{tableId}/{item.Resref}";
                    blueprint.GetProperty("LocalizedName").GetProperty("value").GetProperty("0")
                        .GetString().Should().Contain("Training", source);
                    var bottom = baseItem == BaseItem.Lightsaber ? 151 : 71;
                    ReadModelPart(blueprint, 1).Should().Be(bottom, source);
                    ReadModelPart(blueprint, 2).Should().Be(11, source);
                    ReadModelPart(blueprint, 3).Should().Be(45, source);

                    var prefix = baseItem == BaseItem.Lightsaber ? "wswglsbr" : "wdblsbr";
                    foreach (var modelResref in new[] { $"{prefix}_b_{bottom:D3}", $"{prefix}_m_011", $"{prefix}_t_045" })
                    {
                        if (!models.TryGetValue(modelResref, out var model))
                        {
                            var modelPath = Path.Combine(root.FullName, "SWLOR_Haks", "sw_weapon", $"{modelResref}.mdl");
                            model = new MdlReader().Parse(File.ReadAllBytes(modelPath));
                            models.Add(modelResref, model);
                        }

                        EnumerateNodes(model.GeometryRoot!).OfType<MdlEmitterNode>()
                            .Should().BeEmpty($"{source} must not display an energy blade");
                        if (modelResref.Contains("_b_"))
                        {
                            EnumerateNodes(model.GeometryRoot!).OfType<MdlTrimeshNode>()
                                .Should().Contain(mesh => mesh.Render && mesh.Vertices.Length > 0,
                                    $"{source} must display a solid training weapon");
                        }
                    }

                    trainingWeapons.Add(item.Resref);
                }
            }
        }

        trainingWeapons.Should().HaveCount(21, "all former lightsaber drops have training replacements");
        foreach (var resref in trainingWeapons)
        {
            resref.Should().EndWith("_tr");
            using var training = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json")));
            using var original = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{resref[..^3]}.uti.json")));
            foreach (var field in new[] { "BaseItem", "PropertiesList", "Plot", "Cursed", "StackSize", "Cost", "AddCost" })
            {
                training.RootElement.GetProperty(field).GetRawText().Should()
                    .Be(original.RootElement.GetProperty(field).GetRawText(), $"{resref} preserves the original weapon's {field}");
            }
        }
    }

    [TestCase("wswglsbr", 128)]
    [TestCase("wdblsbr", 256)]
    public void TrainingBladeCaps_AreEmptyModelsWithTransparentInventoryLayers(string prefix, int iconHeight)
    {
        var directory = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_weapon");
        var modelResref = $"{prefix}_t_045";
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(directory, $"{modelResref}.mdl")));
        model.Name.Should().Be(modelResref);
        var nodes = EnumerateNodes(model.GeometryRoot!).ToList();
        nodes.OfType<MdlEmitterNode>().Should().BeEmpty();
        nodes.OfType<MdlTrimeshNode>().Should().BeEmpty();

        var icon = TgaReader.Read(Path.Combine(directory, $"i{modelResref}.tga"));
        icon.Width.Should().Be(32);
        icon.Height.Should().Be(iconHeight);
        icon.Pixels.Where((_, index) => index % 4 == 3).Should().OnlyContain(alpha => alpha == 0);
    }

    private static int ReadModelPart(JsonElement blueprint, int part)
    {
        var field = blueprint.TryGetProperty($"xModelPart{part}", out var extended)
            ? extended
            : blueprint.GetProperty($"ModelPart{part}");
        return field.GetProperty("value").GetInt32();
    }

    private static IEnumerable<MdlNode> EnumerateNodes(MdlNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        foreach (var descendant in EnumerateNodes(child))
            yield return descendant;
    }

    private static IEnumerable<Type> GetLootTableDefinitionTypes()
    {
        return typeof(ILootTableDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(ILootTableDefinition).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsInterface)
            .OrderBy(type => type.Name);
    }

    private static HashSet<string> ReadModuleItemTemplateResrefs(DirectoryInfo root)
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("TemplateResRef", out var templateResRef) &&
                templateResRef.TryGetProperty("value", out var value))
            {
                var resref = value.GetString();
                if (!string.IsNullOrWhiteSpace(resref))
                    resrefs.Add(resref);
            }
        }

        return resrefs;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
