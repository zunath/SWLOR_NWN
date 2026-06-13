using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerSurgeStatusEffect : StatusEffectBase
    {
        public override string Name => "Power Surge";
        public override EffectIconType Icon => EffectIconType.PowerSurgeStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 4f;
        public override bool PersistsOnLogout => false;

        public PowerSurgeStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 6;
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 6;
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, 1);
        }
    }
}
