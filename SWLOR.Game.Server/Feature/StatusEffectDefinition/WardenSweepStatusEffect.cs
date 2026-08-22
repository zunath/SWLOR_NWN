using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Warden Sweep's retaliatory guard: reflects a portion of incoming physical damage back at
    /// the attacker for as long as the guard holds.
    /// </summary>
    public sealed class WardenSweepStatusEffect : StatusEffectBase
    {
        private const int ReflectionPercent = 20;

        public override string Name => "Warden Sweep";
        public override EffectIconType Icon => EffectIconType.WardenSweepStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public WardenSweepStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageReflectionPercentAdjustment] = ReflectionPercent;
        }
    }
}
