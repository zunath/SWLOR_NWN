using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// The ally-facing half of the Warden Wall stance: applied by the stance's aura tick to party
    /// members near the warden, granting the same defensive hardening the warden has. Short-lived;
    /// continuously refreshed while the ally stays in range of an active Warden Wall.
    /// </summary>
    public sealed class WardenWallAuraStatusEffect : StatusEffectBase
    {
        public override string Name => "Warden Wall Aura";
        public override EffectIconType Icon => EffectIconType.WardenWallAuraStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public WardenWallAuraStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
        }
    }
}
