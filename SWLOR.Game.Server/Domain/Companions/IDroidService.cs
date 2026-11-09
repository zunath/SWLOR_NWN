using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Player droids: spawning and despawning from the controller item, the saved equipment and inventory snapshot,
    /// the controller-backed droid data, and the droid's event handlers. Static callers reach the container's instance
    /// through <c>GameServices.Droid</c>; the event handlers are on <see cref="DroidService"/>.
    /// </summary>
    public interface IDroidService
    {
        string GetInstructionDiscValidationError(uint user, uint item);

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
        /// Whether a creature is an enemy or NPC machine (droid, robot, or construct) and so cannot bleed.
        /// Player characters and player droids keep their normal Bleed behavior.
        /// </summary>
        bool IsBloodlessMachine(uint creature);

        /// <summary>
        /// Whether a droid is still being summoned and restoring its saved equipment.
        /// </summary>
        bool IsSpawning(uint droid);

        /// <summary>
        /// Retrieves the controller item associated with a droid.
        /// If not found, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="droid">The droid to check</param>
        /// <returns>The controller item or OBJECT_INVALID.</returns>
        uint GetControllerItem(uint droid);

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
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
        /// Hides a droid's native helmet model before equipping it.
        /// </summary>
        void OnEquipItem();

        /// <summary>
        /// Updates the controller-backed snapshot for an item currently equipped by a droid.
        /// </summary>
        /// <param name="droid">The droid wearing the item.</param>
        /// <param name="item">The equipped item to serialize.</param>
        void UpdateEquippedItemSnapshot(uint droid, uint item);

        /// <summary>
        /// Restores a droid helmet's native model before unequipping it.
        /// </summary>
        void OnUnequipItem();

        /// <summary>
        /// Saves completed equipment changes, including changes without a matching unequip event.
        /// </summary>
        void OnEquipmentChanged();

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

        /// <summary>The controller inventory key a droid item waits under until it can be restored into a slot.</summary>
        string GetPendingItemRestoreKey(InventorySlot slot, string id);

        /// <summary>
        /// When the appearance of a droid is changed, update the data on the local variable.
        /// </summary>
        void EditDroidAppearance();

        /// <summary>
        /// When a player enters space or forcefully removes a droid from the party, the droid gets despawned.
        /// </summary>
        void RemoveAssociate();

        /// <summary>
        /// Saves the droid's equipped and carried items onto its controller, keeping pending restore and return entries.
        /// </summary>
        /// <param name="droid">The droid whose items are saved.</param>
        void SaveItemSnapshot(uint droid);

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
        void SaveConstructedDroid(uint controller, ConstructedDroid constructedDroid);

        void SaveInstructions(uint controller, ConstructedDroid constructedDroid);

        void DroidOnBlocked();

        void DroidOnEndCombatRound();

        void DroidOnConversation();

        void DroidOnDamaged();

        void DroidOnDeath();

        void DroidOnDisturbed();

        void DroidOnHeartbeat();

        void DroidOnPhysicalAttacked();

        void DroidOnRested();

        void DroidOnSpawn();

        void DroidOnSpellCastAt();

        void DroidOnUserDefined();
    }
}
