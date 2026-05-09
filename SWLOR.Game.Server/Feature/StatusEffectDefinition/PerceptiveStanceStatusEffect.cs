using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PerceptiveStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Perceptive Stance";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage)
        {
            var chance = Math.Min(30, 10 + GetPositiveAbilityModifier(AbilityType.Might, attacker));
            if (Random.D100(1) <= chance)
            {
                AssignCommand(defender, () => ClearAllActions());
            }
        }
    }
}
