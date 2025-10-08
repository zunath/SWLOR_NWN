using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterHealingModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterHealingModifierChanged; 
    } 
} 
