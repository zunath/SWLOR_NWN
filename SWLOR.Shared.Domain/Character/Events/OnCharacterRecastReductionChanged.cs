using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterRecastReductionChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterRecastReductionChanged;
    }
}
