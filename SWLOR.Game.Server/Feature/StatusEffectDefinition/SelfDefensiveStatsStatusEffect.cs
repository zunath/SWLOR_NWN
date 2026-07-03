using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SelfDefensiveStatsStatusEffect : StatusEffectBase
    {
        private readonly string _name;
        private readonly EffectIconType _icon;
        private readonly int _evasionPercentAdjustment;
        private readonly int _physicalDefensePercentAdjustment;
        private readonly int _forceDefensePercentAdjustment;

        public override string Name => _name;
        public override EffectIconType Icon => _icon;

        public SelfDefensiveStatsStatusEffect()
            : this(25, 20, 20, "Self Defensive Stats", EffectIconType.Invalid)
        {
        }

        public SelfDefensiveStatsStatusEffect(
            int evasionPercentAdjustment,
            int physicalDefensePercentAdjustment,
            int forceDefensePercentAdjustment,
            string name = "Self Defensive Stats",
            EffectIconType icon = EffectIconType.Invalid)
        {
            _name = name;
            _icon = icon;
            _evasionPercentAdjustment = evasionPercentAdjustment;
            _physicalDefensePercentAdjustment = physicalDefensePercentAdjustment;
            _forceDefensePercentAdjustment = forceDefensePercentAdjustment;

            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = _physicalDefensePercentAdjustment;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = _forceDefensePercentAdjustment;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = _evasionPercentAdjustment;
        }

        public override IStatusEffect Clone()
        {
            return new SelfDefensiveStatsStatusEffect(
                _evasionPercentAdjustment,
                _physicalDefensePercentAdjustment,
                _forceDefensePercentAdjustment,
                _name,
                _icon);
        }
    }
}
