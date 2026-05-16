using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressTheAttack2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Press the Attack II";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PressTheAttack3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PressTheAttack1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(6, 8);
        }
    }
}
