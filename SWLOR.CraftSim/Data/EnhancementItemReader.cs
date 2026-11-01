using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Data
{
    /// <summary>
    /// Reads enhancement items from Module/uti the way <c>CraftEnhancementReader</c> and the old window's <c>IsValidEnhancement</c> and
    /// <c>CalculateProgressPenaltyAndProcessItemProperties</c> do: an item is an enhancement of the kind its enhancement item property
    /// names, with an <c>EnhancementLevel</c>, and adds the sum of its <c>ProgressPenalty</c> cost-table values to the craft.
    /// </summary>
    public static class EnhancementItemReader
    {
        private static readonly Regex HasProgressPenalty = new(
            "\"PropertyName\"\\s*:\\s*\\{\\s*\"type\"\\s*:\\s*\"word\"\\s*,\\s*\"value\"\\s*:\\s*" + (int)ItemPropertyType.ProgressPenalty + "\\s*\\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>The item property that makes an item an enhancement for a kind of recipe (CraftViewModel.cs:601-628).</summary>
        public static readonly IReadOnlyDictionary<ItemPropertyType, RecipeEnhancementType> KindByProperty =
            new Dictionary<ItemPropertyType, RecipeEnhancementType>
            {
                [ItemPropertyType.ArmorEnhancement] = RecipeEnhancementType.Armor,
                [ItemPropertyType.WeaponEnhancement] = RecipeEnhancementType.Weapon,
                [ItemPropertyType.StructureEnhancement] = RecipeEnhancementType.Structure,
                [ItemPropertyType.FoodEnhancement] = RecipeEnhancementType.Food,
                [ItemPropertyType.StarshipEnhancement] = RecipeEnhancementType.Starship,
                [ItemPropertyType.ModuleEnhancement] = RecipeEnhancementType.Module,
                [ItemPropertyType.DroidEnhancement] = RecipeEnhancementType.Droid
            };

        /// <summary>The result of reading the module's items.</summary>
        /// <param name="Items">Enhancement items with exactly one enhancement property, a level and a progress penalty.</param>
        /// <param name="Skipped">Items with a progress penalty that are not enhancements this model can use, with the reason.</param>
        public sealed record ReadResult(IReadOnlyList<EnhancementItem> Items, IReadOnlyList<string> Skipped);

        public static ReadResult ReadAll(string itemFolder)
        {
            if (!Directory.Exists(itemFolder))
                throw new DirectoryNotFoundException($"Cannot find the module item folder {itemFolder}.");

            var items = new List<EnhancementItem>();
            var skipped = new List<string>();
            foreach (var file in Directory.EnumerateFiles(itemFolder, "*.uti.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                var text = File.ReadAllText(file, Encoding.Latin1);
                if (!HasProgressPenalty.IsMatch(text))
                    continue;

                JObject root;
                try
                {
                    root = JObject.Parse(text);
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"{Path.GetFileName(file)} is not valid item JSON: {ex.Message}", ex);
                }

                var resref = Path.GetFileName(file)[..^".uti.json".Length];
                var item = Read(resref, root, out var reason);
                if (item != null)
                    items.Add(item);
                else if (reason != null)
                    skipped.Add($"{resref}: {reason}");
            }

            return new ReadResult(items, skipped);
        }

        /// <summary>One item's enhancement reading, or null with the reason it is not an enhancement the model can use.</summary>
        public static EnhancementItem Read(string resref, JObject root, out string reason)
        {
            reason = null;
            if (root["PropertiesList"]?["value"] is not JArray properties)
                return null;

            var name = root["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? resref;
            var penalty = 0;
            var hasPenalty = false;
            var level = -1;
            var kinds = new List<RecipeEnhancementType>();
            foreach (var property in properties)
            {
                var type = (ItemPropertyType)(property["PropertyName"]?["value"]?.Value<int>() ?? -1);
                var amount = property["CostValue"]?["value"]?.Value<int>() ?? 0;
                if (type == ItemPropertyType.ProgressPenalty)
                {
                    hasPenalty = true;
                    penalty += amount;
                }
                else if (type == ItemPropertyType.EnhancementLevel)
                {
                    level = amount;
                }
                else if (KindByProperty.TryGetValue(type, out var kind))
                {
                    kinds.Add(kind);
                }
            }

            if (!hasPenalty)
                return null;

            if (kinds.Count == 0 || level < 0)
            {
                reason = kinds.Count == 0 ? "has a progress penalty but no enhancement property" : "has no enhancement level";
                return null;
            }

            if (kinds.Count > 1)
            {
                // Each property rolls its own transfer in the old code, so such an item is not one slot's worth of transfer.
                reason = $"has {kinds.Count} enhancement properties, so it is not one enhancement";
                return null;
            }

            return new EnhancementItem(resref, name, kinds[0], level, penalty);
        }
    }
}
