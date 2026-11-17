using SWLOR.Game.Server.Domain.Administration;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The AI profile read the DM object details make (<see cref="IAIProfileDescription"/>, owned by Administration; P5-15a of
    /// Readmes/MaintainabilityRefactorPhase5.md): the profile <see cref="IAIService.GetAIProfile"/> returns, printed.
    /// </summary>
    public class AIProfileDescription : IAIProfileDescription
    {
        private readonly IAIService _ai;

        public AIProfileDescription(IAIService ai)
        {
            _ai = ai;
        }

        public string DescribeAIProfile(uint creature) => _ai.GetAIProfile(creature).ToString();
    }
}
