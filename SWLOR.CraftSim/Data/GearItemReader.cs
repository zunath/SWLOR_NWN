using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Data
{
    /// <summary>Reads crafting stats from Module/uti as EquipmentStats.cs:929-1075 applies them; CraftingGearCostTableTests checks that the cost value is the amount.</summary>
    public static class GearItemReader
    {
        public const int ControlProperty = 88;
        public const int CraftsmanshipProperty = 89;
        public const int CpBonusProperty = 115;

        private static readonly SkillType[] SkillBySubtype =
        {
            SkillType.Invalid, SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture
        };

        public static IReadOnlyList<GearItem> ReadAll(string itemFolder)
        {
            if (!Directory.Exists(itemFolder))
                throw new DirectoryNotFoundException($"Cannot find the module item folder {itemFolder}.");

            var items = new List<GearItem>();
            foreach (var file in Directory.EnumerateFiles(itemFolder, "*.uti.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                var text = File.ReadAllText(file, Encoding.Latin1);

                // Nearly every item has properties, so skip the parse unless a crafting stat can appear in the file.
                if (!text.Contains("\"PropertyName\"", StringComparison.Ordinal))
                    continue;

                JObject root;
                try
                {
                    root = JObject.Parse(text);
                }
                catch (JsonException)
                {
                    continue;
                }

                items.AddRange(Read(Path.GetFileName(file)[..^".uti.json".Length], root));
            }

            return items;
        }

        public static IEnumerable<GearItem> Read(string resref, JObject root)
        {
            if (root["PropertiesList"]?["value"] is not JArray properties)
                yield break;

            var baseItem = (BaseItem)(root["BaseItem"]?["value"]?.Value<int>() ?? 0);
            var name = root["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? resref;
            var totals = new Dictionary<SkillType, int[]>();

            foreach (var property in properties)
            {
                var type = property["PropertyName"]?["value"]?.Value<int>() ?? -1;
                if (type != ControlProperty && type != CraftsmanshipProperty && type != CpBonusProperty)
                    continue;

                var subtype = property["Subtype"]?["value"]?.Value<int>() ?? 0;
                if (subtype < 1 || subtype >= SkillBySubtype.Length)
                    continue;

                var amount = property["CostValue"]?["value"]?.Value<int>() ?? 0;
                var skill = SkillBySubtype[subtype];
                if (!totals.TryGetValue(skill, out var sums))
                    totals[skill] = sums = new int[3];

                sums[type == ControlProperty ? 0 : type == CraftsmanshipProperty ? 1 : 2] += amount;
            }

            foreach (var (skill, sums) in totals.OrderBy(t => (int)t.Key))
            {
                var item = new GearItem(resref, name, baseItem, skill, sums[0], sums[1], sums[2]);
                if (item.HasStats)
                    yield return item;
            }
        }
    }
}
