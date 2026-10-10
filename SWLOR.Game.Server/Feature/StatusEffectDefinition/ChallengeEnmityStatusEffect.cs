using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ChallengeEnmityStatusEffect : StatusEffectBase
    {
        private readonly int _percent;
        public override string Name => "Challenge";
        public override EffectIconType Icon => EffectIconType.ChallengeEnmityStatusEffect;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public ChallengeEnmityStatusEffect() : this(0) { }

        public ChallengeEnmityStatusEffect(int percent)
        {
            _percent = percent;
            StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = percent;
        }

        public override IStatusEffect Clone() => new ChallengeEnmityStatusEffect(_percent);
    }
}
