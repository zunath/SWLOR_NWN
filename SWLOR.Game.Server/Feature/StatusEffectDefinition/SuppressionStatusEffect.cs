using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SuppressionStatusEffect : StatusEffectBase, IStatusEffectRemovedHandler
    {
        public override string Name => "Suppression";
        public override EffectIconType Icon => EffectIconType.SuppressionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override StatusEffectStackType StackingType => StatusEffectStackType.UnlimitedStacking;

        public int EvasionPenaltyPercent { get; }

        public SuppressionStatusEffect()
            : this(0)
        {
        }

        public SuppressionStatusEffect(int evasionPenaltyPercent)
        {
            EvasionPenaltyPercent = evasionPenaltyPercent;

            if (EvasionPenaltyPercent > 0)
                StatGroup.Stats[StatType.EvasionPercentAdjustment] = -EvasionPenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new SuppressionStatusEffect(EvasionPenaltyPercent);
        }

        public void AfterRemoved(uint creature)
        {
            Combat.ReconcileContainmentNetStatus(Source, creature);
        }
    }
}
