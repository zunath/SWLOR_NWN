using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SmokeRoundCoverStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPercent;

        public override string Name => "Smoke Round Cover";
        public override EffectIconType Icon => EffectIconType.SmokeRoundCoverStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public SmokeRoundCoverStatusEffect()
            : this(15)
        {
        }

        public SmokeRoundCoverStatusEffect(int evasionPercent)
        {
            _evasionPercent = Math.Max(0, evasionPercent);
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = _evasionPercent;
        }

        public override IStatusEffect Clone()
        {
            return new SmokeRoundCoverStatusEffect(_evasionPercent);
        }
    }
}
