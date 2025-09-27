using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
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
