using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The rewards an elite or boss wreck's salvage rare find can be instead of its tier table's item (plan section 14,
    /// Appendix D): a Design Schematic, a Ship Schematic or a stack of rare torpedoes. Schematics are for ships of the
    /// wreck's tier or lower; Rift Raider schematics only come from Wild Space.
    /// </summary>
    public static class ShipRareFindRules
    {
        /// <summary>
        /// Rolls what a rare find is. <paramref name="next"/> returns a number from 0 to its argument minus 1. Returns
        /// the item resrefs to deliver (one entry per item), or an empty list when the rare find is the table's item.
        /// </summary>
        public static List<string> Roll(int wreckTier, WreckDifficulty difficulty, Func<int, int> next)
        {
            var rewards = new List<string>();
            if (!RevampRewardCatalog.RareFindRewards.TryGetValue(difficulty, out var odds))
                return rewards;

            var tier = Math.Clamp(wreckTier, 1, 5);
            var roll = next(100);
            if (roll < odds.DesignPercent)
            {
                var designs = Designs(tier);
                if (designs.Count > 0)
                    rewards.Add(designs[next(designs.Count)].Resref);
            }
            else if (roll < odds.DesignPercent + odds.ShipPercent)
            {
                var schematics = ShipSchematics(tier);
                if (schematics.Count > 0)
                    rewards.Add(schematics[next(schematics.Count)].Resref);
            }
            else if (roll < odds.DesignPercent + odds.ShipPercent + odds.TorpedoPercent)
            {
                var torpedoes = RareTorpedoes();
                if (torpedoes.Count > 0)
                    rewards.AddRange(Enumerable.Repeat(torpedoes[next(torpedoes.Count)].Resref, RevampRewardCatalog.RareTorpedoQuantity));
            }

            return rewards;
        }

        /// <summary>The Hull Designs a wreck of this tier can give the schematic of.</summary>
        public static List<ShipHullDesign> Designs(int tier) =>
            RevampRewardCatalog.HullDesigns.Where(x => !x.WildSpaceOnly && x.Tier <= tier).ToList();

        /// <summary>The Ship Schematics a wreck of this tier can give.</summary>
        public static List<ShipSchematic> ShipSchematics(int tier) =>
            RevampRewardCatalog.ShipSchematics.Where(x => !x.WildSpaceOnly && x.Tier <= tier).ToList();

        /// <summary>The torpedo variants only salvage gives.</summary>
        public static List<ShipAmmoVariant> RareTorpedoes() =>
            RevampRewardCatalog.AmmoVariants.Where(x => x.Launcher == ShipModuleFamily.TorpedoLauncher && !x.Crafted).ToList();
    }
}
