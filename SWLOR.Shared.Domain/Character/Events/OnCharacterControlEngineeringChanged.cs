using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterControlEngineeringChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterControlEngineeringChanged; 
    } 
} 
