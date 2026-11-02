using System.Text;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>One kind of vein or tree a harvester works: the module placeable, its loot table, its tier and whether it can hold gems.</summary>
    /// <param name="Resref">The placeable's resref, such as <c>veldite_vein</c>.</param>
    /// <param name="Table">The harvesting loot table its <c>HARVESTING_LOOT_TABLE</c> variable names.</param>
    /// <param name="Tier">Its <c>HARVESTER_REQUIRED_LEVEL</c>, 1 to 5.</param>
    /// <param name="HasGems">True when the table has gem entries, so the node can hold gem pockets.</param>
    /// <param name="GemChance">The share of a roll of the table that is a gem: the weight of its gem entries over its whole weight.</param>
    public sealed record HarvestNodeType(string Resref, string Table, int Tier, bool HasGems, double GemChance);

    /// <summary>The veins and trees of the module, read from the placeables themselves and the harvesting loot tables.</summary>
    public static class HarvestNodeCatalog
    {
        private const string TableVariable = "HARVESTING_LOOT_TABLE";
        private const string LevelVariable = "HARVESTER_REQUIRED_LEVEL";

        /// <summary>Every placeable in the folder that names a harvesting loot table, in resref order.</summary>
        public static IReadOnlyList<HarvestNodeType> Read(string placeableFolder)
        {
            if (!Directory.Exists(placeableFolder))
                throw new DirectoryNotFoundException($"Cannot find the module placeable folder {placeableFolder}.");

            var tables = new HarvestingLootTableDefinition().BuildLootTables();
            var nodes = new List<HarvestNodeType>();
            foreach (var file in Directory.EnumerateFiles(placeableFolder, "*.utp.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                var text = File.ReadAllText(file, Encoding.Latin1);
                if (!text.Contains(TableVariable, StringComparison.Ordinal))
                    continue;

                var root = JObject.Parse(text);
                string table = null;
                var tier = 0;
                foreach (var variable in root["VarTable"]?["value"] as JArray ?? new JArray())
                {
                    var name = variable["Name"]?["value"]?.Value<string>();
                    var value = variable["Value"]?["value"];
                    if (name == TableVariable)
                        table = value?.Value<string>();
                    else if (name == LevelVariable)
                        tier = value?.Value<int>() ?? 0;
                }

                if (string.IsNullOrEmpty(table) || tier < 1)
                    continue;

                if (!tables.TryGetValue(table, out LootTable lootTable))
                    throw new InvalidDataException($"{Path.GetFileName(file)} names the loot table {table}, which no definition builds.");

                var totalWeight = lootTable.Sum(item => (double)item.Weight);
                var gemWeight = ProspectingGems.GemEntries(lootTable).Sum(item => (double)item.Weight);
                nodes.Add(new HarvestNodeType(
                    Path.GetFileName(file)[..^".utp.json".Length], table, tier, ProspectingGems.HasGems(lootTable),
                    totalWeight <= 0 ? 0 : gemWeight / totalWeight));
            }

            return nodes;
        }
    }
}
