using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DecisiveCommand1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Decisive Command";
        public override EffectIconType Icon => EffectIconType.DecisiveCommand1StatusEffect;
        public override bool PersistsOnLogout => false;
        public override float Frequency => 3f;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(24, 30);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = ScaleBySourceSocial(10, 12);
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = ScaleBySourceSocial(10, 12);
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, 1);
        }
    }
}
