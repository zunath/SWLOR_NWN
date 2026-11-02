using Newtonsoft.Json;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>One purchasable perk level as the Design Bible's Gathering tab lists it.</summary>
    public sealed record GatheringPerkBibleRow(
        string Line, int Price, string Name, int Rank, string Type, string Description, IReadOnlyList<string> Stats);

    /// <summary>
    /// The Prospector and Treasure Seeker rows of the Design Bible's Gathering tab, read from the game's own perk definitions so the
    /// workbook cannot drift from the code. <c>tools/UpdateGatheringPerkBible.py --data</c> writes them into the workbook, and
    /// <c>SWLOR.CraftSim gathering-perks --part bible --out rows.json</c> exports them.
    /// </summary>
    public static class GatheringPerkBibleRows
    {
        private static readonly string[] Roman = { "I", "II", "III" };

        public static IReadOnlyList<GatheringPerkBibleRow> Read(IReadOnlyDictionary<PerkType, PerkDetail> perks)
        {
            var rows = new List<GatheringPerkBibleRow>();
            foreach (var (line, members) in new[] { ("Prospector", GatheringPerkLines.Prospector), ("Treasure Seeker", GatheringPerkLines.TreasureSeeker) })
            {
                var lineRows = new List<GatheringPerkBibleRow>();
                foreach (var type in members)
                {
                    var detail = perks[type];
                    foreach (var (level, perkLevel) in detail.PerkLevels.OrderBy(pair => pair.Key))
                    {
                        var name = detail.PerkLevels.Count > 1 ? $"{detail.Name} {Roman[level - 1]}" : detail.Name;
                        var stats = perkLevel.StatBonuses.Select(bonus => $"{bonus.Stat} {bonus.Calculate(0):+#;-#;0}").ToList();
                        var isStyle = type is PerkType.StripMiningStyle or PerkType.CarefulHandsStyle;
                        lineRows.Add(new GatheringPerkBibleRow(
                            line, perkLevel.Price, name, GatheringPerkLines.RequiredRank(perkLevel), isStyle ? "Style" : "Trait", perkLevel.Description, stats));
                    }
                }

                rows.AddRange(lineRows.OrderBy(row => row.Rank));
            }

            return rows;
        }

        public static string ToJson(IReadOnlyList<GatheringPerkBibleRow> rows) =>
            JsonConvert.SerializeObject(new { rows }, Formatting.Indented);
    }
}
