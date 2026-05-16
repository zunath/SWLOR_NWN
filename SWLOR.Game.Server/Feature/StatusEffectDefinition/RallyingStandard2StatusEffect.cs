using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RallyingStandard2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Rallying Standard II";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(RallyingStandard1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = ScaleBySourceSocial(5, 6);
        }
    }
}
