using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ComprehendSpeechStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            ComprehendSpeech();

            return _builder.Build();
        }

        private void ComprehendSpeech()
        {
            _builder.Create(StatusEffectType.ComprehendSpeech1)
                .Name("Comprehend Speech I")
                .EffectIcon(EffectIconType.SkillIncrease);

            _builder.Create(StatusEffectType.ComprehendSpeech2)
                .Name("Comprehend Speech II")
                .EffectIcon(EffectIconType.SkillIncrease);

            _builder.Create(StatusEffectType.ComprehendSpeech3)
                .Name("Comprehend Speech III")
                .EffectIcon(EffectIconType.SkillIncrease);

            _builder.Create(StatusEffectType.ComprehendSpeech4)
                .Name("Comprehend Speech IV")
                .EffectIcon(EffectIconType.SkillIncrease);
        }
    }
}
