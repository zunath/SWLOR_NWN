using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BlazingSpikesStatusEffect : StatusEffectBase
    {
        public override string Name => "Blazing Spikes";
        public override EffectIconType Icon => EffectIconType.ElementalShield;

        protected override void OnDamageTaken(uint defender, uint attacker, int damage)
        {
            var percent = Math.Min(40, 10 + GetPositiveAbilityModifier(AbilityType.Might, defender));
            ApplyEffectToObject(DurationType.Instant, EffectDamage(PercentOfDamage(damage, percent)), attacker);
        }
    }
}
