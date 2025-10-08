using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterQueuedDMGBonusChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterQueuedDMGBonusChanged; 
    } 
} 
