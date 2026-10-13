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
        // FP returns over the first 20 seconds (10 ticks of 4%); the accuracy bonus lasts the whole effect.
        public const int FPRestorePercentPerTick = 4;
        public const int FPRestoreTicks = 10;

        private int _fpRestoreTicksApplied;

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
            if (_fpRestoreTicksApplied >= FPRestoreTicks)
                return;

            _fpRestoreTicksApplied++;
            var amount = GameMath.PercentOf(Stat.GetMaxFP(creature), FPRestorePercentPerTick);
            Stat.RestoreFP(creature, amount);
        }
    }
}
