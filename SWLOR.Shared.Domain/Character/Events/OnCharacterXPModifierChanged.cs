using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterXPModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterXPModifierChanged; 
    } 
} 
