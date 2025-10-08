using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterSocialChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterSocialChanged; 
    } 
} 
