using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterVitalityChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterVitalityChanged; 
    } 
} 
