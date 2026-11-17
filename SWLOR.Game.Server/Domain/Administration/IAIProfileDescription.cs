namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// The AI profile of a creature as the DM object details show it. Administration owns the interface and Creatures answers
    /// it (<c>AIProfileDescription</c>; supply rule 2 of MaintainabilityRefactorDomainOrder.md, P5-15a), so the details need
    /// nothing from the AI service.
    /// </summary>
    public interface IAIProfileDescription
    {
        /// <summary>The name of the AI profile a creature runs, as the profile's enum value prints.</summary>
        /// <param name="creature">The creature to describe.</param>
        string DescribeAIProfile(uint creature);
    }
}
