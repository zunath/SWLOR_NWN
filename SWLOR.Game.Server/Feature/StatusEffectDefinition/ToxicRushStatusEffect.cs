using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ToxicRushStatusEffect : StatusEffectBase
    {
        public override string Name => "Toxic Rush";
        public override EffectIconType Icon => EffectIconType.Haste;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            if (StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
                Stat.RestoreStamina(attacker, 2);
        }
        public ToxicRushStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 30;
        }

    }
}
