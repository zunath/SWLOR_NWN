using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Perfect Soresu (Master of Soresu capstone): replaces Saber Ward. Converts 40% of incoming physical
    /// damage into Force damage, grants +8% Defense, +12% Force Defense, and +25% Enmity, is treated as five
    /// Soresu Pressure stacks (an additional +10% Defense and +10% Force Defense), and pushes Deflecting
    /// Return to its capstone reflection of 24% up to 75% of normal weapon damage.
    /// </summary>
    public sealed class PerfectSoresuStatusEffect : StatusEffectBase
    {
        public const int ConversionPercent = 40;
        public const int TreatedAsPressureStacks = 5;

        public override string Name => "Perfect Soresu";
        public override EffectIconType Icon => EffectIconType.ForceWardingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public PerfectSoresuStatusEffect()
        {
            // Base capstone buff plus five Soresu Pressure stacks (2% Defense / 2% Force Defense each).
            StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent] = ConversionPercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 8 + TreatedAsPressureStacks * 2;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 12 + TreatedAsPressureStacks * 2;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 25;

            // Deflecting Return is driven to the capstone ceiling of 24% / 75% by the shared reflection reader.
            StatGroup.Stats[StatType.RangedDeflectionReflectionPercent] = 24;
            StatGroup.Stats[StatType.RangedDeflectionReflectionCapPercent] = 75;
        }
    }
}
