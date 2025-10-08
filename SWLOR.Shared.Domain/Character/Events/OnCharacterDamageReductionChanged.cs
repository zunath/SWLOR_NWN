using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterDamageReductionChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterDamageReductionChanged; 
    } 
} 
