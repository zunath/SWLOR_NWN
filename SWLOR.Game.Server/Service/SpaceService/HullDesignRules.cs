using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Hull Designs (plan section 7) as pure functions: which designs a ship model has, which a character has unlocked
    /// with Design Schematics, what a Refit costs and which appearance a ship shows.
    /// </summary>
    public static class HullDesignRules
    {
        /// <summary>The design every ship always has: its own base model.</summary>
        public const string StandardDesignName = "Standard";

        private static readonly Dictionary<string, ShipHullDesign> _byResref =
            RevampRewardCatalog.HullDesigns.ToDictionary(x => x.Resref);

        /// <summary>The design a Design Schematic unlocks, or null when the resref is not one.</summary>
        public static ShipHullDesign FindBySchematic(string resref) =>
            resref != null && _byResref.TryGetValue(resref, out var design) ? design : null;

        /// <summary>Every design of a ship model, in catalog order (its base model is not one of them).</summary>
        public static List<ShipHullDesign> DesignsFor(string shipTag) =>
            RevampRewardCatalog.HullDesigns.Where(x => x.ShipTag == shipTag).ToList();

        /// <summary>How a character's unlocks store a design.</summary>
        public static string Key(ShipHullDesign design) => $"{design.ShipTag}:{(int)design.Appearance}";

        /// <summary>The designs of a ship model a character has unlocked.</summary>
        public static List<ShipHullDesign> UnlockedFor(string shipTag, IReadOnlyDictionary<string, DateTime> unlocked) =>
            DesignsFor(shipTag).Where(x => unlocked != null && unlocked.ContainsKey(Key(x))).ToList();

        /// <summary>Records a design as unlocked. False when the character already had it.</summary>
        public static bool Unlock(IDictionary<string, DateTime> unlocked, ShipHullDesign design, DateTime now)
        {
            var key = Key(design);
            if (unlocked.ContainsKey(key))
                return false;

            unlocked[key] = now;
            return true;
        }

        /// <summary>
        /// Whether a ship of this model may be refitted to the appearance: its own base model (the Standard design)
        /// always, another design only once the character has unlocked it.
        /// </summary>
        public static bool CanRefitTo(
            string shipTag,
            AppearanceType baseAppearance,
            AppearanceType appearance,
            IReadOnlyDictionary<string, DateTime> unlocked)
        {
            if (appearance == baseAppearance)
                return true;

            return UnlockedFor(shipTag, unlocked).Any(x => x.Appearance == appearance);
        }

        /// <summary>
        /// The appearance a ship shows: its refitted design when that design belongs to the ship's model, otherwise its
        /// base model. A ship that was never refitted stores 0.
        /// </summary>
        public static AppearanceType ShipAppearance(string shipTag, AppearanceType baseAppearance, int designAppearance)
        {
            if (designAppearance <= 0)
                return baseAppearance;

            var design = DesignsFor(shipTag).FirstOrDefault(x => (int)x.Appearance == designAppearance);
            return design?.Appearance ?? baseAppearance;
        }

        /// <summary>The credits a Refit to another design costs for a hull of this size.</summary>
        public static int RefitFee(ShipSize size) =>
            RevampRewardCatalog.RefitFee.TryGetValue(size == ShipSize.Invalid ? ShipSize.Small : size, out var fee) ? fee : 0;

        /// <summary>The name a design shows in Ship Management, for example "Pattern A".</summary>
        public static string DesignName(string shipTag, AppearanceType baseAppearance, AppearanceType appearance)
        {
            if (appearance == baseAppearance)
                return StandardDesignName;

            return DesignsFor(shipTag).FirstOrDefault(x => x.Appearance == appearance)?.Name ?? StandardDesignName;
        }

        /// <summary>Every appearance a design can give a ship, so the space code can tell a ship model from a character.</summary>
        public static IEnumerable<AppearanceType> AllDesignAppearances() =>
            RevampRewardCatalog.HullDesigns.Select(x => x.Appearance);
    }
}
