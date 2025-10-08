using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterRecastReductionModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterRecastReductionModifierChanged; 
    } 
} 
