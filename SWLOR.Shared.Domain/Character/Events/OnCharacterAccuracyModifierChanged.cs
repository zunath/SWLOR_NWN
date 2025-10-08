using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterAccuracyModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterAccuracyModifierChanged; 
    } 
} 
