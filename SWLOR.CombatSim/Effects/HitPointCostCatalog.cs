using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// HP costs paid at impact, transcribed from the Heavy Vibroblade definitions under
    /// SWLOR.Game.Server/Feature/AbilityDefinition/HeavyVibroblade. Update this table when those numbers change.
    /// </summary>
    public static class HitPointCostCatalog
    {
        private static readonly Dictionary<FeatType, HitPointCost> Costs = new()
        {
            [FeatType.SoulBurst1] = new HitPointCost { BasePercent = 40, MinimumPercent = 10 },
            [FeatType.SoulStorm1] = new HitPointCost { BasePercent = 40, MinimumPercent = 10 },
            [FeatType.SacrificialBlade1] = new HitPointCost { BasePercent = 8, MinimumPercent = 8 }
        };

        public static bool TryGet(FeatType feat, out HitPointCost cost) => Costs.TryGetValue(feat, out cost);
    }
}
