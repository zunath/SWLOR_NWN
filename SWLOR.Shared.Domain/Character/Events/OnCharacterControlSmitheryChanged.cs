using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterControlSmitheryChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterControlSmitheryChanged; 
    } 
} 
