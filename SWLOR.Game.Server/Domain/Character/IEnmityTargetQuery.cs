namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Who a creature hates most, as the stat code asks it: the out-of-combat regeneration of an AI creature heals only while
    /// nobody is on its enmity table. Character owns the question and the enmity service answers it; the AI heartbeat hands its
    /// enmity service to the regeneration, so the stat code takes no service above Character.
    /// </summary>
    public interface IEnmityTargetQuery
    {
        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        uint GetHighestEnmityTarget(uint enemy);
    }
}
