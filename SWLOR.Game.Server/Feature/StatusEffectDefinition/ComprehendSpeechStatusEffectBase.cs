using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ComprehendSpeechStatusEffectBase : StaticStatStatusEffectBase
    {
        protected ComprehendSpeechStatusEffectBase(int comprehensionBonus)
            : base(StatType.LanguageComprehension, comprehensionBonus)
        {
        }

        public override EffectIconType Icon => EffectIconType.SkillIncrease;
        public override bool PersistsOnLogout => false;
    }
}
