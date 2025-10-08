using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterAccuracyChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterAccuracyChanged; 
    } 
} 
