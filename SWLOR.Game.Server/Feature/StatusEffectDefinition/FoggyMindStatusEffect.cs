using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FoggyMindStatusEffect : StatusEffectBase
    {
        private readonly int _activationDelaySeconds;

        public FoggyMindStatusEffect()
            : this(0)
        {
        }

        public FoggyMindStatusEffect(int activationDelaySeconds)
        {
            _activationDelaySeconds = activationDelaySeconds;
            StatGroup.Stats[StatType.ActivationDelayFlatAdjustment] = activationDelaySeconds;
        }

        public override string Name => "Foggy Mind";
        public override EffectIconType Icon => EffectIconType.Confused;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override IStatusEffect Clone()
        {
            return new FoggyMindStatusEffect(_activationDelaySeconds);
        }
    }
}
