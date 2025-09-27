using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

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
                .EffectIcon(EffectIconType.SkillIncrease)
                .CannotReplace(StatusEffectType.ComprehendSpeech2, StatusEffectType.ComprehendSpeech3, StatusEffectType.ComprehendSpeech4);

            _builder.Create(StatusEffectType.ComprehendSpeech2)
                .Name("Comprehend Speech II")
                .EffectIcon(EffectIconType.SkillIncrease)
                .CannotReplace(StatusEffectType.ComprehendSpeech3, StatusEffectType.ComprehendSpeech4)
                .Replaces(StatusEffectType.ComprehendSpeech1);

            _builder.Create(StatusEffectType.ComprehendSpeech3)
                .Name("Comprehend Speech III")
                .EffectIcon(EffectIconType.SkillIncrease)
                .CannotReplace(StatusEffectType.ComprehendSpeech4)
                .Replaces(StatusEffectType.ComprehendSpeech1, StatusEffectType.ComprehendSpeech2);

            _builder.Create(StatusEffectType.ComprehendSpeech4)
                .Name("Comprehend Speech IV")
                .EffectIcon(EffectIconType.SkillIncrease)
                .Replaces(StatusEffectType.ComprehendSpeech1, StatusEffectType.ComprehendSpeech2, StatusEffectType.ComprehendSpeech3);
        }
    }
}
