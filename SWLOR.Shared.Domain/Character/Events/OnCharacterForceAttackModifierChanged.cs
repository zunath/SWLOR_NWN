using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterForceAttackModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterForceAttackModifierChanged; 
    } 
} 
