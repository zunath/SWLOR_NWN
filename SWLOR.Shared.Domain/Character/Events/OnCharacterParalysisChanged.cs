using SWLOR.Shared.Abstractions; 
 
namespace SWLOR.Shared.Domain.Character.Events 
{ 
    public class OnCharacterParalysisChanged : BaseEvent 
    { 
        public override string Script => CharacterScriptName.OnCharacterParalysisChanged; 
    } 
} 
