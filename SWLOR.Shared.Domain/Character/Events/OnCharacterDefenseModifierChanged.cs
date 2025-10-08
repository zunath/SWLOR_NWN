using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterDefenseModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterDefenseModifierChanged; 
    } 
} 
