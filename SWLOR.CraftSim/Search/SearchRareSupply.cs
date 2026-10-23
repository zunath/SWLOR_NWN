using System.Reflection;
using SWLOR.CraftSim.Gates;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// Evidence, not a gate: how many of a site's finds are its rare finds. Quick Search rolls the whole table as scavenging always did,
    /// so its rare finds are its rolls times the share of the table that is rare (what Treasure Hunter raises). The board adds Dig Deeper,
    /// whose finds are all rare finds, in place of the searches it costs. The table shows what that does to the supply of the rare items
    /// of real scavenge sites, for the owner to judge; the plan's cap is on the rolls, not on what they roll.
    /// </summary>
    public static class SearchRareSupply
    {
        /// <summary>The loot tables of the game by name, from every loot-table definition.</summary>
        public static IReadOnlyDictionary<string, LootTable> LoadTables(IEnumerable<string> names)
        {
            var wanted = new HashSet<string>(names, StringComparer.Ordinal);
            var found = new Dictionary<string, LootTable>(StringComparer.Ordinal);
            foreach (var type in typeof(LootTable).Assembly.GetTypes().Where(type =>
                         typeof(ILootTableDefinition).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false } &&
                         type.GetConstructor(Type.EmptyTypes) != null))
            {
                var definition = (ILootTableDefinition)Activator.CreateInstance(type)!;
                foreach (var (name, table) in definition.BuildLootTables())
                {
                    if (wanted.Contains(name))
                        found[name] = table;
                }
            }

            return found;
        }

        public static GateTable Build(SearchParity parity, SearchParityOptions options, int rank, int treasureHunterLevel, SearchEffects effects = null, string perks = "no perks")
        {
            var tables = LoadTables(options.RareSupplyTables);
            var tier = CraftRankMilestones.HardLookLevelForRank(rank);
            var quickRolls = ScavengeQuickSearch.ExpectedRolls(tier);
            var careless = parity.Sample(rank, 1, SearchPlayerKind.Careless);
            effects ??= SearchEffects.None;
            var skilled = parity.Sample(rank, 1, SearchPlayerKind.Skilled, effects);

            var table = new EvidenceTable(
                "Loot table", "Entries", "Rare finds", "Share of a roll that is rare", "Quick Search per site", "Careless per site", "Skilled per site",
                "Careless / Quick", "Skilled / Quick");
            foreach (var name in options.RareSupplyTables.Where(tables.ContainsKey))
            {
                var loot = tables[name];
                var share = ScavengeLoot.RareShare(loot, treasureHunterLevel, false);
                var deeperShare = ScavengeLoot.RareShare(loot, treasureHunterLevel, true);
                var skilledDeeperShare = ScavengeLoot.RareShare(loot, treasureHunterLevel, true, effects.DeeperWeightBonus);
                var quick = quickRolls * share;
                var carelessRare = careless.Ordinary * share + careless.Deeper * deeperShare;
                var skilledRare = skilled.Ordinary * share + skilled.Deeper * skilledDeeperShare;
                var flagged = loot.Count(item => item.IsRare);
                table.Add(
                    name,
                    loot.Count.ToString(),
                    flagged > 0 ? $"{flagged} flagged" : "lowest weight",
                    Percent(share, 1),
                    Number(quick, 3),
                    Number(carelessRare, 3),
                    Number(skilledRare, 3),
                    quick <= 0 ? "n/a" : Number(carelessRare / quick, 1) + "x",
                    quick <= 0 ? "n/a" : Number(skilledRare / quick, 1) + "x");
            }

            return table.ToGateTable(
                $"Rare finds per site at Gathering rank {rank} (Hard Look level {tier}) with Treasure Hunter {treasureHunterLevel}; the skilled player has {perks}: " +
                "Quick Search against the board");
        }
    }
}
