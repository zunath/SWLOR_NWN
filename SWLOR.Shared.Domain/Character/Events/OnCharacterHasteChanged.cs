using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterHasteChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterHasteChanged; 
    } 
} 
