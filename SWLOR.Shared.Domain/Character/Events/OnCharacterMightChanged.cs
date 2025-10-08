using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterMightChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterMightChanged; 
    } 
} 
