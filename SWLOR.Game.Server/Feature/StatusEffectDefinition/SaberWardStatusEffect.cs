using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Saber Ward: while active, converts a percentage of incoming physical damage into Force damage
    /// (so it is then mitigated by Force Defense) and grants flat Defense and Force Defense.
    /// Only the highest rank applies; Saber Ward and Perfect Aegis replace one another.
    /// </summary>
    public sealed class SaberWardStatusEffect : StatusEffectBase
    {
        private readonly int _conversionPercent;
        private readonly int _defensePercent;
        private readonly int _forceDefensePercent;

        public override string Name => "Saber Ward";
        public override EffectIconType Icon => EffectIconType.SaberWardStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public SaberWardStatusEffect()
            : this(15, 3, 4)
        {
        }

        public SaberWardStatusEffect(int conversionPercent, int defensePercent, int forceDefensePercent)
        {
            _conversionPercent = conversionPercent;
            _defensePercent = defensePercent;
            _forceDefensePercent = forceDefensePercent;

            StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent] = conversionPercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = defensePercent;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = forceDefensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new SaberWardStatusEffect(_conversionPercent, _defensePercent, _forceDefensePercent);
        }
    }
}
