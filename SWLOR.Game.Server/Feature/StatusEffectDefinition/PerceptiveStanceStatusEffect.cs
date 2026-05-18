using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PerceptiveStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Perceptive Stance";
        public override EffectIconType Icon => EffectIconType.PerceptiveStanceStatusEffect;

        public PerceptiveStanceStatusEffect()
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 10;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 15;
        }

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            var chance = Math.Min(30, 10 + Math.Max(0, GetAbilityScore(attacker, AbilityType.Perception)));
            if (Random.D100(1) <= chance)
            {
                AssignCommand(defender, () => ClearAllActions());
            }
        }
    }
}
