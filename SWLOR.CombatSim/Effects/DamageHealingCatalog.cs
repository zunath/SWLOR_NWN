using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// Damage-derived self-heals, transcribed from ForceDrainAbilityDefinition. Update this table when those numbers change.
    /// </summary>
    public static class DamageHealingCatalog
    {
        private static readonly Dictionary<FeatType, DamageHealing> Healing = new()
        {
            [FeatType.ForceDrain1] = new DamageHealing { Percent = 30, LowHpPercent = 40 },
            [FeatType.ForceDrain2] = new DamageHealing { Percent = 35, LowHpPercent = 45 },
            [FeatType.ForceDrain3] = new DamageHealing { Percent = 40, LowHpPercent = 50 }
        };

        public static bool TryGet(FeatType feat, out DamageHealing healing) => Healing.TryGetValue(feat, out healing);
    }
}
