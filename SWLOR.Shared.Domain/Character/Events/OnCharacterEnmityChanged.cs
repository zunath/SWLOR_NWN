using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterEnmityChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterEnmityChanged; 
    } 
} 
