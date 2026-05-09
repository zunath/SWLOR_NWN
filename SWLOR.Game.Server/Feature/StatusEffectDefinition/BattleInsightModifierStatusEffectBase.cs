using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class BattleInsightModifierStatusEffectBase : StatusEffectBase
    {
        protected abstract int AccuracyAdjustment { get; }
        protected abstract int DefenseAdjustment { get; }

        public override string Name => "Battle Insight";
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;

        public override EffectIconType Icon => AccuracyAdjustment >= 0
            ? EffectIconType.AttackIncrease
            : EffectIconType.AttackDecrease;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.Accuracy] = AccuracyAdjustment;
            StatGroup.Stats[StatType.Defense] = DefenseAdjustment;
        }
    }
}
