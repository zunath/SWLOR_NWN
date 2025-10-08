using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterAttackModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterAttackModifierChanged; 
    } 
} 
