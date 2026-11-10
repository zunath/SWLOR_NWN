using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CobraStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Cobra Stance";
        public override EffectIconType Icon => EffectIconType.CobraStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            if (Random.D100(1) <= 10)
                StatusEffect.ApplyStatusEffect(attacker, defender, typeof(PoisonStatusEffect), 30f);
        }
        public CobraStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 10;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -15;
        }

    }
}
