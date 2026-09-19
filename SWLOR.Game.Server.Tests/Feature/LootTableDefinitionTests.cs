using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service.LootService;

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
    public void AlchemizedFrog_GuaranteesTheGutsRequiredForQuestHandIn()
    {
        var trophy = new KorribanLootTableDefinition().BuildLootTables()["FROG_BOSS_TROPHY"];
        trophy.IsRare.Should().BeFalse();
        var guts = trophy.Should().ContainSingle().Which;
        guts.Resref.Should().Be("frogguts");
        guts.Weight.Should().BeGreaterThan(0);
        guts.MaxQuantity.Should().Be(1);

        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "utc", "frogboss.utc.json")));
        var locals = blueprint.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray();
        locals.Where(local => local.GetProperty("Name").GetProperty("value").GetString() == "LOOT_TABLE_6")
            .Should().ContainSingle().Which.GetProperty("Value").GetProperty("value").GetString()
            .Should().Be("FROG_BOSS_TROPHY,100,1");
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
