using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterForceDefenseChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterForceDefenseChanged; 
    } 
} 
