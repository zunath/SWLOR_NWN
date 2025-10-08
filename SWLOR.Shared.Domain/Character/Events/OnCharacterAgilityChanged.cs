using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterAgilityChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterAgilityChanged; 
    } 
} 
