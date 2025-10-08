using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterFPRestoreOnHitChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterFPRestoreOnHitChanged; 
    } 
} 
