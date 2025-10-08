using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterControlAgricultureChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterControlAgricultureChanged; 
    } 
} 
