using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterSlowChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterSlowChanged; 
    } 
} 
