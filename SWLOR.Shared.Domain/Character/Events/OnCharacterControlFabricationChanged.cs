using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterControlFabricationChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterControlFabricationChanged; 
    } 
} 
