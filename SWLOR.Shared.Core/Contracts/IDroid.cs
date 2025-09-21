using SWLOR.Game.Server.Service.DroidService;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IDroid
    {
        /// <summary>
        /// When the module loads, cache all relevant droid data into memory.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Determines if a creature is a droid.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if droid, false otherwise</returns>
        bool IsDroid(uint creature);

        /// <summary>
        /// Retrieves the controller item associated with a droid.
        /// If not found, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="droid">The droid to check</param>
        /// <returns>The controller item or OBJECT_INVALID.</returns>
        uint GetControllerItem(uint droid);

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
        /// Player will receive an error if they don't have any ranks in the Droid Assembly perk.
        /// </summary>
        void UseDroidAssemblyTerminal();

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        void OnPlayerExit();

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        void OnAcquireItem();

        /// <summary>
        /// When a droid loses an item, it is removed from the persistent variable on the controller item.
        /// </summary>
        void OnLostItem();

        /// <summary>
        /// When a droid equips an item, it is removed from its inventory and added to its equipped items.
        /// </summary>
        void OnEquipItem();

        /// <summary>
        /// When a droid unequips an item, it is removed from its equipped items and added to its inventory.
        /// </summary>
        void OnUnequipItem();

        /// <summary>
        /// Loads item property details from a droid's controller item.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>Droid item property details.</returns>
        DroidItemPropertyDetails LoadDroidItemPropertyDetails(uint controller);

        /// <summary>
        /// Loads item property details from a droid part item.
        /// </summary>
        /// <param name="item">The part item to read from</param>
        /// <returns>Droid part item property details</returns>
        DroidPartItemPropertyDetails LoadDroidPartItemPropertyDetails(uint item);

        /// <summary>
        /// Retrieves the droid assigned to a player.
        /// Returns OBJECT_INVALID if a droid is not assigned.
        /// </summary>
        /// <param name="player">The player to retrieve from</param>
        /// <returns>The droid object or OBJECT_INVALID.</returns>
        uint GetDroid(uint player);

        /// <summary>
        /// Spawns a droid NPC based on details found on the controller item.
        /// </summary>
        /// <param name="player">The player spawning the droid.</param>
        /// <param name="controller">The controller item</param>
        void SpawnDroid(uint player, uint controller);

        /// <summary>
        /// When the appearance of a droid is changed, update the data on the local variable.
        /// </summary>
        void EditDroidAppearance();

        /// <summary>
        /// When a player enters space or forcefully removes a droid from the party, the droid gets despawned.
        /// </summary>
        void RemoveAssociate();

        /// <summary>
        /// Loads constructed droid information stored as a local variable on the controller item.
        /// If this doesn't exist, a new object will be returned.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>A ConstructedDroid object.</returns>
        ConstructedDroid LoadConstructedDroid(uint controller);

        /// <summary>
        /// Saves constructed droid information onto a local variable on the controller item.
        /// </summary>
        /// <param name="controller">The controller item to write to.</param>
        /// <param name="constructedDroid">The constructed droid data to save.</param>
        void SaveConstructedDroid(uint controller, object constructedDroid);

        void DroidOnBlocked();
        void DroidOnEndCombatRound();
        void DroidOnConversation();
        void DroidOnDamaged();
        void DroidOnDeath();
        void DroidOnDisturbed();
        void DroidOnHeartbeat();
        void DroidOnPerception();
        void DroidOnPhysicalAttacked();
        void DroidOnRested();
        void DroidOnSpawn();
        void DroidOnSpellCastAt();
        void DroidOnUserDefined();
    }
}