using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterAttackDeflectionChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterAttackDeflectionChanged; 
    } 
} 
