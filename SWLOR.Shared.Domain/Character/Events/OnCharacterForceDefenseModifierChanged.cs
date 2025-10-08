using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterForceDefenseModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterForceDefenseModifierChanged; 
    } 
} 
