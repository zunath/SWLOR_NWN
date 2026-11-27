using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Creatures
{
    public sealed class AIPhaseDefinition
    {
        public AIPhaseId Id { get; set; }
        public AIPhaseCondition EnterCondition { get; set; }
        public List<AIActionDefinition> Actions { get; } = new();
    }
}
