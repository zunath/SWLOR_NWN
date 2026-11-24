namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The creature event entry points: heartbeat, combat round end, conversation, attacked, damaged, death, disturbed, spawn,
    /// blocked and the DM AI toggle. The handlers are on <see cref="AICreatureEventService"/>.
    /// </summary>
    public interface IAICreatureEventService
    {
        /// <summary>
        /// After a DM toggles the AI of a creature off, its AI state is cleared.
        /// </summary>
        void DMToggleAIAfter();

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        void CreatureHeartbeat();

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        void CreatureCombatRoundEnd();

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        void CreatureConversation();

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        void CreaturePhysicalAttacked();

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        void CreatureDamaged();

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        void CreatureDeath();

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        void CreatureDisturbed();

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        void CreatureSpawn();

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        void CreatureRested();

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        void CreatureSpellCastAt();

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        void CreatureUserDefined();

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        void CreatureBlocked();
    }
}
