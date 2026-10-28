using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Manufacturer components (plan section 14, Appendix D) as pure functions: which maker an NPC ship's wreck belongs
    /// to and the salvage roll for that maker's component. A manufacturer component is the family's tier 5 item with
    /// the maker's trait, so every tier 5 recipe of the family takes it.
    /// </summary>
    public static class ShipManufacturerRules
    {
        private static readonly Dictionary<ShipManufacturer, ShipManufacturerComponent> _components =
            RevampComponentCatalog.ManufacturerComponents.ToDictionary(x => x.Manufacturer);

        private static readonly Dictionary<string, ShipManufacturer> _aces =
            RevampAceCatalog.Aces.ToDictionary(x => x.Tag, x => x.Manufacturer);

        /// <summary>The maker's component, or null when the maker has none.</summary>
        public static ShipManufacturerComponent Component(ShipManufacturer manufacturer) =>
            _components.TryGetValue(manufacturer, out var component) ? component : null;

        /// <summary>
        /// The maker an NPC ship's wreck yields components of (Appendix D, "Loot table changes"): Republic and Czerka
        /// customs patrols their own; the Sith customs, the Imperial fleet (tier 6) and the Sith capitals Sith; Clan Zel
        /// Arun (tier 4) and the Mandalorian capitals Mandalorian; pirate raiders and the other faction tiers (the
        /// Khanate, Krakenborne, Snowdrift Syndicate and Red Hand pirates) Corellian; the Rift Raiders (Wild Space)
        /// Rift Raider; an ace its own maker. Invalid for anything else, such as a derelict.
        /// </summary>
        public static ShipManufacturer WreckManufacturer(string creatureTag)
        {
            var tag = (creatureTag ?? string.Empty).ToLowerInvariant();
            if (_aces.TryGetValue(tag, out var ace))
                return ace;
            if (WildSpaceRules.IsRiftRaider(tag))
                return ShipManufacturer.RiftRaider;
            if (tag.Contains("repcus"))
                return ShipManufacturer.Republic;
            if (tag.Contains("czkcus"))
                return ShipManufacturer.Czerka;
            if (tag.Contains("sithcus") || tag.StartsWith("sithcap"))
                return ShipManufacturer.Sith;
            if (tag.StartsWith("mandocap"))
                return ShipManufacturer.Mandalorian;
            if (tag.Length < 2 || tag[0] != 't' || !char.IsDigit(tag[1]))
                return ShipManufacturer.Invalid;
            if (tag.Contains("pir"))
                return ShipManufacturer.Corellian;

            return tag[1] switch
            {
                '4' => ShipManufacturer.Mandalorian,
                '6' => ShipManufacturer.Sith,
                _ => ShipManufacturer.Corellian,
            };
        }

        /// <summary>The chance per salvage cycle (0-100) of the wreck's maker component.</summary>
        public static int SalvagePercent(WreckDifficulty difficulty) =>
            RevampComponentCatalog.ManufacturerSalvagePercent.TryGetValue(difficulty, out var percent) ? percent : 0;

        /// <summary>
        /// Rolls one salvage cycle's manufacturer component. <paramref name="next"/> returns a number from 0 to its argument
        /// minus 1. Null when the wreck has no maker or the roll misses.
        /// </summary>
        public static (string Resref, ShipComponentTraitType Trait)? RollSalvage(
            ShipManufacturer manufacturer,
            WreckDifficulty difficulty,
            Func<int, int> next)
        {
            var component = Component(manufacturer);
            if (component == null || next(100) >= SalvagePercent(difficulty))
                return null;

            return (component.Resref, component.Trait);
        }
    }
}
