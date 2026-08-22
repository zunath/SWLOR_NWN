using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Perfect Aegis (Aegis Eternal capstone): replaces Saber Ward. Converts 40% of incoming physical
    /// damage into Force damage, grants +8% Defense, +12% Force Defense, and +25% Enmity, is treated as five
    /// Embattled stacks (an additional +10% Defense and +10% Force Defense), and pushes Deflecting
    /// Return to its capstone reflection of 50% up to 125% of normal weapon damage.
    /// </summary>
    public sealed class PerfectAegisStatusEffect : StatusEffectBase
    {
        public const int ConversionPercent = 40;
        public const int TreatedAsEmbattledStacks = 5;
        public const int ReflectionPercent = 50;
        public const int ReflectionCapPercent = 125;

        public override string Name => "Perfect Aegis";
        public override EffectIconType Icon => EffectIconType.PerfectAegisStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public PerfectAegisStatusEffect()
        {
            // Base capstone buff plus five Embattled stacks (2% Defense / 2% Force Defense each).
            StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent] = ConversionPercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 8 + TreatedAsEmbattledStacks * 2;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 12 + TreatedAsEmbattledStacks * 2;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 25;

            // These are final overrides rather than additive bonuses: ordinary perk reflection and
            // Center of the Storm remain present while the capstone status is active.
            StatGroup.Stats[StatType.RangedDeflectionReflectionOverridePercent] = ReflectionPercent;
            StatGroup.Stats[StatType.RangedDeflectionReflectionCapOverridePercent] = ReflectionCapPercent;
        }
    }
}
