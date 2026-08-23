using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Marks a creature as being inside a rifle Kill Box.</summary>
    [StatConfiguredIcon]
    public sealed class KillBoxStatusEffect : StatusEffectBase, IRangedHitSuppressionSource
    {
        public override string Name => "Kill Box";
        public override EffectIconType Icon => EffectIconType.SuppressionStanceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override bool PersistsOnLogout => false;

        public int SuppressionPenaltyPercent { get; }
        public int SuppressionDurationSeconds => 30;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public KillBoxStatusEffect()
            : this(0)
        {
        }

        public KillBoxStatusEffect(int suppressionPenaltyPercent)
        {
            SuppressionPenaltyPercent = suppressionPenaltyPercent;
        }

        public override IStatusEffect Clone() => new KillBoxStatusEffect(SuppressionPenaltyPercent);
    }
}
