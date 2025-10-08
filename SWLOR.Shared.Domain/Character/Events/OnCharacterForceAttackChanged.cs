using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterForceAttackChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterForceAttackChanged; 
    } 
} 
