namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The quest advance the twin pistol trials make when a player earns a proof. Combat owns the interface and
    /// <c>IQuestService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface IQuestAdvancer
    {
        /// <summary>
        /// Makes a player advance to the next state of the quest.
        /// If there are no additional states, the quest will be treated as completed.
        /// </summary>
        /// <param name="player">The player who is advancing to the next state of the quest.</param>
        /// <param name="questSource">The source of the quest. Typically an NPC or object.</param>
        /// <param name="questId">The Id of the quest to advance.</param>
        bool AdvanceQuest(uint player, uint questSource, string questId);
    }
}
