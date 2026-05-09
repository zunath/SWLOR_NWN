using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulAscensionStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Ascension";
        public override EffectIconType Icon => EffectIconType.Regenerate;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            ApplyEffectToObject(DurationType.Instant, EffectHeal(PercentOfDamage(damage, 50)), attacker);
        }
        public SoulAscensionStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 35;
        }

    }
}
