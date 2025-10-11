using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterMovementSpeedChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterMovementSpeedChanged;
    } 
} 
