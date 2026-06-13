using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceConvergenceStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Convergence";
        public override EffectIconType Icon => EffectIconType.ForceConvergenceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 2f;
        public override bool PersistsOnLogout => false;

        public ForceConvergenceStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Force;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 5;
        }

        protected override void Tick(uint creature)
        {
            var amount = Math.Max(1, (int)Math.Ceiling(Stat.GetMaxFP(creature) * 0.04f));
            Stat.RestoreFP(creature, amount);
        }
    }
}
