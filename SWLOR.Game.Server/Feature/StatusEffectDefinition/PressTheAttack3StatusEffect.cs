using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressTheAttack3StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Press the Attack III";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PressTheAttack1StatusEffect),
            typeof(PressTheAttack2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(14, 18);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = ScaleBySourceSocial(5, 7);
        }
    }
}
