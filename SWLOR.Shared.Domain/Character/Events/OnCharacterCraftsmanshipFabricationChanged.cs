using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterCraftsmanshipFabricationChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterCraftsmanshipFabricationChanged; 
    } 
} 
