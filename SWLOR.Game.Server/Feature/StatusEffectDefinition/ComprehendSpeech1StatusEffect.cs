using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ComprehendSpeech1StatusEffect : StatusEffectBase
    {
        public override string Name => "Comprehend Speech";
        public override EffectIconType Icon => EffectIconType.ComprehendSpeech1StatusEffect;
        public override bool PersistsOnLogout => false;

        public ComprehendSpeech1StatusEffect()
        {
            StatGroup.Stats[StatType.LanguageComprehension] = 15;
        }
    }
}
