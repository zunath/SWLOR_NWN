using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The rewards an elite or boss wreck's salvage rare find can be instead of its tier table's item (plan section 14,
    /// Appendix D): a Design Schematic, a Ship Schematic or a stack of rare torpedoes. Schematics are for ships of the
    /// wreck's tier or lower; Rift Raider schematics only come from Wild Space, where a rare find can also be a Module
    /// Schematic or Prototype Tech (plan section 23).
    /// </summary>
    public static class ShipRareFindRules
    {
        /// <summary>
        /// Rolls what a rare find is. <paramref name="next"/> returns a number from 0 to its argument minus 1. Returns
        /// the item resrefs to deliver (one entry per item), or an empty list when the rare find is the table's item.
        /// </summary>
        public static List<string> Roll(int wreckTier, WreckDifficulty difficulty, Func<int, int> next, bool wildSpace = false)
        {
            var rewards = new List<string>();
            if (!RevampRewardCatalog.RareFindRewards.TryGetValue(difficulty, out var odds))
                return rewards;

            var tier = Math.Clamp(wreckTier, 1, 5);
            var roll = next(100);
            if (roll < odds.DesignPercent)
            {
                var designs = Designs(tier, wildSpace);
                if (designs.Count > 0)
                    rewards.Add(designs[next(designs.Count)].Resref);
            }
            else if (roll < odds.DesignPercent + odds.ShipPercent)
            {
                var schematics = ShipSchematics(tier, wildSpace);
                if (schematics.Count > 0)
                    rewards.Add(schematics[next(schematics.Count)].Resref);
            }
            else if (roll < odds.DesignPercent + odds.ShipPercent + odds.TorpedoPercent)
            {
                var torpedoes = RareTorpedoes();
                if (torpedoes.Count > 0)
                    rewards.AddRange(Enumerable.Repeat(torpedoes[next(torpedoes.Count)].Resref, RevampRewardCatalog.RareTorpedoQuantity));
            }
            else if (wildSpace && WildSpaceTuning.RareFindExtras.TryGetValue(difficulty, out var extras))
            {
                var start = odds.DesignPercent + odds.ShipPercent + odds.TorpedoPercent;
                if (roll < start + extras.ModulePercent)
                {
                    var modules = RevampRewardCatalog.ModuleSchematics;
                    if (modules.Count > 0)
                        rewards.Add(modules[next(modules.Count)].Resref);
                }
                else if (roll < start + extras.ModulePercent + extras.PrototypePercent)
                {
                    rewards.Add(RevampRewardCatalog.PrototypeTechResref);
                }
            }

            return rewards;
        }

        /// <summary>The Hull Designs a wreck of this tier can give the schematic of; Wild Space adds the Wild Space only ones.</summary>
        public static List<ShipHullDesign> Designs(int tier, bool wildSpace = false) =>
            RevampRewardCatalog.HullDesigns.Where(x => (wildSpace || !x.WildSpaceOnly) && x.Tier <= tier).ToList();

        /// <summary>The Ship Schematics a wreck of this tier can give; Wild Space adds the Wild Space only ones.</summary>
        public static List<ShipSchematic> ShipSchematics(int tier, bool wildSpace = false) =>
            RevampRewardCatalog.ShipSchematics.Where(x => (wildSpace || !x.WildSpaceOnly) && x.Tier <= tier).ToList();

        /// <summary>The torpedo variants only salvage gives.</summary>
        public static List<ShipAmmoVariant> RareTorpedoes() =>
            RevampRewardCatalog.AmmoVariants.Where(x => x.Launcher == ShipModuleFamily.TorpedoLauncher && !x.Crafted).ToList();
    }
}
