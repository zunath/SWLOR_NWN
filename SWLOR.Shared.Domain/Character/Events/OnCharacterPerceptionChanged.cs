using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterPerceptionChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterPerceptionChanged; 
    } 
} 
