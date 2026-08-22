using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveFootworkStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPercent;

        public override string Name => "Evasive Footwork";
        public override EffectIconType Icon => EffectIconType.EvasiveFootworkStatusEffect;

        public EvasiveFootworkStatusEffect()
            : this(10)
        {
        }

        public EvasiveFootworkStatusEffect(int evasionPercent)
        {
            _evasionPercent = evasionPercent;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = _evasionPercent;
        }

        public override IStatusEffect Clone()
        {
            return new EvasiveFootworkStatusEffect(_evasionPercent);
        }
    }
}
