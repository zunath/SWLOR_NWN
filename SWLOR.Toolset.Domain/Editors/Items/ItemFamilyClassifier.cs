using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Places a baseitems.2da row into an <see cref="ItemFamily"/>. Rules and every label listed
    /// below were checked against the live SWLOR_Haks/sw_2da/baseitems.2da corpus (541 rows, 80 in
    /// use); a label absent from the corpus today (e.g. "saberstaff") is kept for when a matching
    /// row is added, since matching it costs nothing.
    /// </summary>
    public static class ItemFamilyClassifier
    {
        private static readonly HashSet<string> MeleeLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "longsword", "shortsword", "dagger", "greatsword", "greataxe", "battleaxe", "handaxe",
            "katana", "scimitar", "rapier", "bastardsword", "kukri", "sickle", "club", "lightmace",
            "morningstar", "warhammer", "lighthammer", "lightflail", "heavyflail", "halberd",
            "shortspear", "quarterstaff", "twobladedsword", "doubleaxe", "dwarvenwaraxe", "whip",
            "scythe", "trident", "katar", "electroblade", "twinelectroblade"
        };

        private static readonly HashSet<string> RangedLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "pistol", "rifle", "longbow", "shortbow", "lightcrossbow", "heavycrossbow", "sling",
            "dart", "shuriken", "throwingaxe", "grenade", "cannon"
        };

        private static readonly HashSet<string> AccessoryLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "ring", "amulet", "belt", "bracer", "gloves", "boots"
        };

        private static readonly HashSet<string> ToolLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "fishingrod", "holdable", "holdable2", "fashionacc", "flowers"
        };

        public static ItemFamily Classify(BaseItemRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            return Classify(row.Id, row.Label, row.ModelType);
        }

        /// <summary>
        /// <paramref name="modelType"/> is accepted for callers that already have it to hand, but
        /// every row in the corpus is presently decidable from <paramref name="label"/> alone; it
        /// is reserved for a future tie-break rather than read today.
        /// </summary>
        public static ItemFamily Classify(int baseItemId, string label, int modelType)
        {
            var text = (label ?? string.Empty).Trim();

            if (Contains(text, "lightsaber") || Contains(text, "saberstaff"))
                return ItemFamily.Lightsaber;

            if (MeleeLabels.Contains(text))
                return ItemFamily.MeleeWeapon;

            if (RangedLabels.Contains(text) || Contains(text, "smallarms"))
                return ItemFamily.RangedWeapon;

            if (string.Equals(text, "armor", StringComparison.OrdinalIgnoreCase))
                return ItemFamily.Armor;

            if (string.Equals(text, "helmet", StringComparison.OrdinalIgnoreCase))
                return ItemFamily.Helmet;

            if (string.Equals(text, "cloak", StringComparison.OrdinalIgnoreCase))
                return ItemFamily.Cape;

            if (Contains(text, "shield"))
                return ItemFamily.Shield;

            if (AccessoryLabels.Contains(text))
                return ItemFamily.Accessory;

            if (ToolLabels.Contains(text))
                return ItemFamily.Tool;

            if (StartsWith(text, "creature") ||
                string.Equals(text, "cslshprcweap", StringComparison.OrdinalIgnoreCase) ||
                (StartsWith(text, "c") && EndsWith(text, "weapon")))
                return ItemFamily.CreatureItem;

            if (StartsWith(text, "ess"))
                return ItemFamily.Essence;

            return ItemFamily.Miscellaneous;
        }

        private static bool Contains(string text, string value) =>
            text.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool StartsWith(string text, string value) =>
            text.StartsWith(value, StringComparison.OrdinalIgnoreCase);

        private static bool EndsWith(string text, string value) =>
            text.EndsWith(value, StringComparison.OrdinalIgnoreCase);
    }
}
