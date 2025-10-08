using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterLevelChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterLevelChanged; 
    } 
} 
