using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterExtraAttackModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterExtraAttackModifierChanged; 
    } 
} 
