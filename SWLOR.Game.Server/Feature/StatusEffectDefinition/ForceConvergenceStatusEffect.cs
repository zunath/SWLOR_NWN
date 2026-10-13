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
        // 40% of max FP returns over the first 20 seconds (10 ticks); the accuracy bonus lasts the whole effect.
        public const int FPRestorePercent = 40;
        public const int FPRestoreTicks = 10;

        private int _fpRestoreTicksApplied;
        private int _fpRestoreTotal;

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

            if (_fpRestoreTicksApplied == 0)
                _fpRestoreTotal = GameMath.PercentOf(Stat.GetMaxFP(creature), FPRestorePercent);

            var restoredBefore = GetFPRestoredThroughTick(_fpRestoreTotal, _fpRestoreTicksApplied);
            _fpRestoreTicksApplied++;
            var amount = GetFPRestoredThroughTick(_fpRestoreTotal, _fpRestoreTicksApplied) - restoredBefore;
            if (amount > 0)
                Stat.RestoreFP(creature, amount);
        }

        /// <summary>FP restored after <paramref name="ticks"/> ticks, so the ten ticks add up to exactly the total.</summary>
        public static int GetFPRestoredThroughTick(int totalRestore, int ticks)
        {
            return totalRestore * Math.Clamp(ticks, 0, FPRestoreTicks) / FPRestoreTicks;
        }
    }
}
