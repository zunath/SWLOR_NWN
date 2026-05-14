using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressTheAttack1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Press the Attack I";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PressTheAttack2StatusEffect),
            typeof(PressTheAttack3StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = ScaleBySourceSocial(4, 6);
        }
    }
}
