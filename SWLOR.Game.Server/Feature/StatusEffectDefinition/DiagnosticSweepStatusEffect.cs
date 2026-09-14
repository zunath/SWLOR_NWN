using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DiagnosticSweepStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPenaltyPercent;

        public override string Name => "Diagnostic Sweep";
        public override EffectIconType Icon => EffectIconType.DiagnosticSweepStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override bool PersistsOnLogout => false;

        public DiagnosticSweepStatusEffect()
            : this(4)
        {
        }

        public DiagnosticSweepStatusEffect(int evasionPenaltyPercent)
        {
            _evasionPenaltyPercent = evasionPenaltyPercent;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -_evasionPenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new DiagnosticSweepStatusEffect(_evasionPenaltyPercent);
        }
    }
}
