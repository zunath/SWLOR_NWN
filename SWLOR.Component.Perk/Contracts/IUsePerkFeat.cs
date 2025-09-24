namespace SWLOR.Component.Perk.Contracts
{
    /// <summary>
    /// Interface for handling perk feat usage and related functionality.
    /// </summary>
    public interface IUsePerkFeat
    {
        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        void UseFeat();

        /// <summary>
        /// When a weapon hits, process any queued weapon abilities.
        /// </summary>
        void ProcessQueuedWeaponAbility();

        /// <summary>
        /// When a player enters the module, clear any temporary queued variables.
        /// </summary>
        void ClearTemporaryQueuedVariables();

        /// <summary>
        /// When a player starts resting, clear any temporary queued variables.
        /// </summary>
        void ClearTemporaryQueuedVariablesOnRest();

        /// <summary>
        /// When a player equips an item, clear any temporary queued variables.
        /// </summary>
        void ClearTemporaryQueuedVariablesOnEquip();
    }
}
