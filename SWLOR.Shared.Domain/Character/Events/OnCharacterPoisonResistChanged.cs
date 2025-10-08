using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterPoisonResistChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterPoisonResistChanged; 
    } 
} 
