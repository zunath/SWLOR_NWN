using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FoggyMindStatusEffect : StatusEffectBase
    {
        private const int DefaultActivationDelaySeconds = 1;

        private readonly int _activationDelaySeconds;
        private readonly int _abilityHitChancePenaltyPercent;

        public FoggyMindStatusEffect()
            : this(DefaultActivationDelaySeconds)
        {
        }

        public FoggyMindStatusEffect(int activationDelaySeconds)
            : this(activationDelaySeconds, 0)
        {
        }

        public FoggyMindStatusEffect(int activationDelaySeconds, int abilityHitChancePenaltyPercent)
        {
            _activationDelaySeconds = activationDelaySeconds;
            _abilityHitChancePenaltyPercent = Math.Abs(abilityHitChancePenaltyPercent);
            StatGroup.Stats[StatType.ActivationDelayFlatAdjustment] = activationDelaySeconds;
            if (_abilityHitChancePenaltyPercent > 0)
                StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -_abilityHitChancePenaltyPercent;
        }

        public override string Name => "Foggy Mind";
        public override EffectIconType Icon => EffectIconType.FoggyMindStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override IStatusEffect Clone()
        {
            return new FoggyMindStatusEffect(_activationDelaySeconds, _abilityHitChancePenaltyPercent);
        }
    }
}
