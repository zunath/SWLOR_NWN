namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// The beast event handlers, the acquire check and the stables terminal. Static callers reach the container's instance
    /// through <c>GameServices.BeastEvent</c>.
    /// </summary>
    public interface IBeastEventService
    {
        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        void RemoveAssociate();

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        void OnAcquireItem();

        /// <summary>
        /// A beast's blocked-by-door event.
        /// </summary>
        void BeastOnBlocked();

        /// <summary>
        /// A beast's end of combat round event.
        /// </summary>
        void BeastOnEndCombatRound();

        /// <summary>
        /// A beast's dialogue event.
        /// </summary>
        void BeastOnConversation();

        /// <summary>
        /// A beast's damaged event.
        /// </summary>
        void BeastOnDamaged();

        /// <summary>
        /// A beast's death event: records the death on its master and marks the beast dead.
        /// </summary>
        void BeastOnDeath();

        /// <summary>
        /// A beast's inventory disturbed event.
        /// </summary>
        void BeastOnDisturbed();

        /// <summary>
        /// A beast's heartbeat event.
        /// </summary>
        void BeastOnHeartbeat();

        /// <summary>
        /// A beast's melee attacked event.
        /// </summary>
        void BeastOnPhysicalAttacked();

        /// <summary>
        /// A beast's rested event.
        /// </summary>
        void BeastOnRested();

        /// <summary>
        /// A beast's spawn event.
        /// </summary>
        void BeastOnSpawn();

        /// <summary>
        /// A beast's spell cast at event.
        /// </summary>
        void BeastOnSpellCastAt();

        /// <summary>
        /// A beast's user defined event.
        /// </summary>
        void BeastOnUserDefined();

        /// <summary>
        /// When a player uses the stables terminal, the stables window opens.
        /// </summary>
        void OpenStablesMenu();
    }
}
