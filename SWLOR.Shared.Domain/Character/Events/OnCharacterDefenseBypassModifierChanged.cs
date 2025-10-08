using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterDefenseBypassModifierChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterDefenseBypassModifierChanged; 
    } 
} 
