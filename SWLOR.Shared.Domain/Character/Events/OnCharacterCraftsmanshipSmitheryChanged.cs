using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterCraftsmanshipSmitheryChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterCraftsmanshipSmitheryChanged; 
    } 
} 
