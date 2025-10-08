using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterCraftsmanshipAgricultureChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterCraftsmanshipAgricultureChanged; 
    } 
} 
