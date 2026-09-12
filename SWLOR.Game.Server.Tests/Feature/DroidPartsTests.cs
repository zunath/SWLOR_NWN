using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

public class DroidPartsTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    [Test]
    public void EveryGeneratedPartMatchesItsSourceStatsAndHasACraftingRecipe()
    {
        var recipes = typeof(IRecipeListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IRecipeListDefinition).IsAssignableFrom(type) &&
                           type.Name.StartsWith("Droid", StringComparison.Ordinal))
            .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes().Values)
            .Select(recipe => recipe.Resref).ToHashSet();
        var rows = File.ReadAllLines(Path.Combine(Root, "SWLOR.CLI", "InputFiles", "droid_item_list.tsv"))
            .Select(line => line.Split('\t')).Where(row => row.Length >= 4 && row[2] != "CPU").ToArray();
        rows.Should().NotBeEmpty();
        var partTypes = new Dictionary<string, int> { ["Head"] = 2, ["Body"] = 3, ["Arms"] = 4, ["Legs"] = 5 };
        foreach (var row in rows)
        {
            var name = row[1];
            var item = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", name + ".uti.json")));
            var properties = item["PropertiesList"]!["value"]!.Children().ToArray();
            properties.Single(property => (int)property["PropertyName"]!["value"]! == (int)ItemPropertyType.DroidPart)
                ["Subtype"]!["value"]!.Value<int>().Should().Be(partTypes[row[2]], name);
            var stats = properties.Where(property => (int)property["PropertyName"]!["value"]! == (int)ItemPropertyType.DroidStat)
                .GroupBy(property => (int)property["Subtype"]!["value"]!)
                .ToDictionary(group => group.Key, group => group.Sum(property => (int)property["CostValue"]!["value"]!));
            stats[(int)DroidStatSubType.Tier].Should().Be(int.Parse(row[3]), name);
            stats.GetValueOrDefault((int)DroidStatSubType.AISlots).Should().Be(Value(row, 4), name);
            for (var column = 8; column <= 13; column++)
                stats.GetValueOrDefault(column - 2).Should().Be(Value(row, column), name);
            stats.Keys.Should().OnlyContain(key => Enum.IsDefined((DroidStatSubType)key) && key != 0, name);
            recipes.Should().Contain(name, "every generated part needs a player crafting source");
        }
    }

    [Test]
    public void CurrentDroidStatsNeverReuseLegacyWeaponIdsAndMatchHakLabels()
    {
        var rows = File.ReadAllLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "iprp_droidstat.2da"))
            .Select(line => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            .Where(row => row.Length >= 4 && int.TryParse(row[0], out _)).ToDictionary(row => int.Parse(row[0]));
        foreach (var type in Enum.GetValues<DroidStatSubType>().Where(type => type != DroidStatSubType.Invalid))
        {
            ((int)type).Should().NotBeInRange(12, 15, "legacy saved weapon groups must never become resistance properties");
            rows[(int)type][1].Should().NotBe("****", $"{type} requires a player-facing TLK label");
            if (type.ToString().StartsWith("Resistance", StringComparison.Ordinal))
                rows[(int)type][2].Should().Be(type.ToString());
        }
    }

    private static int Value(string[] row, int column) => column < row.Length && int.TryParse(row[column], out var value) ? value : 0;
}
