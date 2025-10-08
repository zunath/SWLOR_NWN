using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterWillpowerChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterWillpowerChanged; 
    } 
} 
