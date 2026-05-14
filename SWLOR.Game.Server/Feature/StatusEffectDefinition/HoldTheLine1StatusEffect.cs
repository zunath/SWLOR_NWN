using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HoldTheLine1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Hold the Line";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -ScaleBySourceSocial(25, 30);
            StatGroup.Stats[StatType.MindResistance] = 100;
            StatGroup.Stats[StatType.MobilityResistance] = 100;
        }
    }
}
