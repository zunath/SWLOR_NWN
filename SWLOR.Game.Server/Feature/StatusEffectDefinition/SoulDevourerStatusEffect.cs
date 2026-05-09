using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulDevourerStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Devourer";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            var percent = Math.Max(10, 40 - GetPositiveAbilityModifier(AbilityType.Might, attacker));
            ApplyEffectToObject(DurationType.Instant, EffectDamage(PercentOfDamage(damage, percent)), attacker);
        }
        public SoulDevourerStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 35;
        }

    }
}
