using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Data
{
    /// <summary>Derives the gear levels from the real gear: Smithery recipes make every skill's wearable gear, ordered by recipe level into tiers 1-5 per slot.</summary>
    public sealed class CraftingGearCatalog
    {
        /// <summary>The body slots a crafting set fills, with the number of items worn in each.</summary>
        public static readonly IReadOnlyDictionary<BaseItem, int> SlotCounts = new Dictionary<BaseItem, int>
        {
            { BaseItem.Armor, 1 },
            { BaseItem.Helmet, 1 },
            { BaseItem.Amulet, 1 },
            { BaseItem.Belt, 1 },
            { BaseItem.Boots, 1 },
            { BaseItem.Ring, 2 },
            { BaseItem.Bracer, 1 },
            { BaseItem.Cloak, 1 },
        };

        public const string NoGearName = "no gear";
        public const string MidGearName = "mid gear";
        public const string BestGearName = "best gear";

        private readonly Dictionary<(SkillType Skill, BaseItem Slot, int Tier), GearItem> _tiers = new();
        private readonly List<string> _notes = new();

        private CraftingGearCatalog()
        {
        }

        /// <summary>Gear items that were not placed in a tier, with the reason.</summary>
        public IReadOnlyList<string> Notes => _notes;

        public int TierCount(SkillType skill) =>
            _tiers.Keys.Where(k => k.Skill == skill).Select(k => k.Tier).DefaultIfEmpty(0).Max();

        public static CraftingGearCatalog Build(IEnumerable<GearItem> items, RecipeCatalog recipes)
        {
            var catalog = new CraftingGearCatalog();
            var itemsByResref = items
                .GroupBy(i => (i.Resref.ToLowerInvariant(), i.Skill))
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var skill in GearSkills)
            {
                var made = recipes.ForSkill(GearMaker)
                    .Select(r => (Recipe: r, Item: itemsByResref.GetValueOrDefault((r.Resref?.ToLowerInvariant() ?? string.Empty, skill))))
                    .Where(x => x.Item != null && SlotCounts.ContainsKey(x.Item.BaseItem))
                    .GroupBy(x => x.Item.Resref)
                    .Select(g => g.OrderBy(x => x.Recipe.Level).First())
                    .ToList();

                foreach (var slot in made.GroupBy(x => x.Item.BaseItem))
                {
                    var ordered = slot.OrderBy(x => x.Recipe.Level).ThenBy(x => x.Item.Resref, StringComparer.Ordinal).ToList();
                    for (var index = 0; index < ordered.Count; index++)
                        catalog._tiers[(skill, slot.Key, index + 1)] = ordered[index].Item;
                }
            }

            catalog.CheckTiers();
            return catalog;
        }

        /// <summary>A set worn at a tier: the tier's item in every slot that has one.</summary>
        public GearLevel Tier(SkillType skill, int tier, string name = null)
        {
            var worn = new List<GearItem>();
            foreach (var (slot, count) in SlotCounts)
            {
                if (!_tiers.TryGetValue((skill, slot, tier), out var item))
                    continue;

                for (var copy = 0; copy < count; copy++)
                    worn.Add(item);
            }

            return new GearLevel(name ?? $"tier {tier} set", skill, tier, worn);
        }

        /// <summary>No gear, the middle tier and the best tier of each skill that has crafting gear.</summary>
        public IReadOnlyList<GearLevel> Levels(SkillType skill)
        {
            var levels = new List<GearLevel> { GearLevel.None(skill) };
            var best = TierCount(skill);
            if (best <= 0)
                return levels;

            var mid = (best + 1) / 2;
            if (mid < best)
                levels.Add(Tier(skill, mid, MidGearName));
            levels.Add(Tier(skill, best, BestGearName));
            return levels;
        }

        /// <summary>The skill whose recipes make the player's wearable crafting gear.</summary>
        public const SkillType GearMaker = SkillType.Smithery;

        public static readonly IReadOnlyList<SkillType> GearSkills = new[]
        {
            SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture
        };

        private void CheckTiers()
        {
            foreach (var skill in GearSkills)
            {
                var best = TierCount(skill);
                foreach (var slot in SlotCounts.Keys)
                {
                    var tiers = _tiers.Keys.Where(k => k.Skill == skill && k.Slot == slot).Select(k => k.Tier).ToList();
                    if (tiers.Count != best)
                        _notes.Add($"{skill} {slot} has {tiers.Count} tiers where the skill's best tier is {best}.");
                }
            }
        }
    }
}
