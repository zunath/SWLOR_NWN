using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterShieldDeflectionChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterShieldDeflectionChanged; 
    } 
} 
