using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Beast companions: the beast catalog, taming and leveling, spawning a beast for its master, the creature event
    /// handlers of a spawned beast, and the incubation of DNA into beast eggs.
    /// <c>Service.BeastMastery</c> is the static facade; every member of it forwards to this interface.
    /// </summary>
    public interface IBeastMasteryService
    {
        /// <summary>
        /// When the module caches, loads the beast catalog, the beast roles, the foods, the highest XP delta and the
        /// incubation percentages.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Retrieves the catalog entry of a beast type.
        /// </summary>
        /// <param name="type">The type of beast</param>
        /// <returns>The details of the beast type</returns>
        BeastDetail GetBeastDetail(BeastType type);

        /// <summary>
        /// Retrieves every beast type the catalog has loaded.
        /// </summary>
        IEnumerable<BeastType> GetAllBeastTypes();

        /// <summary>
        /// Retrieves the details of a beast role.
        /// </summary>
        /// <param name="type">The role</param>
        BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type);

        /// <summary>
        /// Retrieves the id of the beast entity a spawned beast was made from.
        /// </summary>
        /// <param name="beast">The spawned beast</param>
        string GetBeastId(uint beast);

        /// <summary>
        /// Stores the id of the beast entity on a spawned beast.
        /// </summary>
        /// <param name="beast">The spawned beast</param>
        /// <param name="beastId">The id of the beast entity</param>
        void SetBeastId(uint beast, string beastId);

        /// <summary>
        /// Retrieves the type of a spawned beast, or <see cref="BeastType.Invalid"/> when the creature is not a beast.
        /// </summary>
        /// <param name="beast">The spawned beast</param>
        BeastType GetBeastType(uint beast);

        /// <summary>
        /// Determines whether a creature is a beast that belongs to a player.
        /// </summary>
        /// <param name="beast">The creature to check</param>
        bool IsPlayerBeast(uint beast);

        /// <summary>
        /// Stores the type of beast on a spawned beast.
        /// </summary>
        /// <param name="beast">The spawned beast</param>
        /// <param name="type">The type of beast</param>
        void SetBeastType(uint beast, BeastType type);

        /// <summary>
        /// Gives XP to a spawned beast, applying the XP bonuses unless they are ignored, then applies its new stats.
        /// </summary>
        /// <param name="beast">The spawned beast</param>
        /// <param name="xp">The XP to give</param>
        /// <param name="ignoreBonuses">true to skip the XP bonuses</param>
        void GiveBeastXP(uint beast, int xp, bool ignoreBonuses);

        /// <summary>
        /// Gives RP XP to one of the player's beasts, whether it is out or in the stables.
        /// The award is capped so no XP is lost, and XP bonuses do not apply.
        /// </summary>
        /// <param name="player">The beast's owner</param>
        /// <param name="beastId">The beast to receive the XP</param>
        /// <param name="xp">The RP XP to give</param>
        /// <returns>The XP the beast accepted</returns>
        int GiveBeastRPXP(uint player, string beastId, int xp);

        /// <summary>
        /// Retrieves the highest level a beast can reach with the given Tame perk level.
        /// </summary>
        int GetBeastLevelCap(int tamePerkLevel);

        /// <summary>
        /// Calculates how much XP a beast can take before it reaches its level cap, so none is lost.
        /// </summary>
        /// <param name="dbBeast">The beast entity</param>
        /// <param name="levelCap">The highest level the beast can reach</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        int GetMaxDistributableXP(Beast dbBeast, int levelCap);

        /// <summary>
        /// Retrieves the XP a beast of a level needs to reach the next level, with its XP penalty applied.
        /// </summary>
        /// <param name="level">The level of the beast</param>
        /// <param name="xpPenalty">The XP penalty percent of the beast</param>
        int GetRequiredXP(int level, int xpPenalty);

        /// <summary>
        /// Determines whether a player is free to spawn a new companion (beast or droid).
        /// The game engine only permits one henchman-type associate at a time, so beast and
        /// droid activation must share this check to prevent an orphaned, master-less companion.
        /// </summary>
        /// <param name="player">The player attempting to spawn a companion.</param>
        /// <returns>An error message if a companion is already active, otherwise an empty string.</returns>
        string GetCompanionSlotValidationError(uint player);

        /// <summary>
        /// Spawns a player's beast next to them and makes it their henchman.
        /// </summary>
        /// <param name="player">The beast's owner</param>
        /// <param name="beastId">The id of the beast entity</param>
        /// <param name="percentHeal">The percent of its hit points the beast is healed to after it spawns</param>
        void SpawnBeast(uint player, string beastId, int percentHeal);

        /// <summary>
        /// Picks two different foods at random: the one a beast likes and the one it hates.
        /// </summary>
        (BeastFoodType, BeastFoodType) GetLikedAndHatedFood();

        /// <summary>
        /// When a player's beast is in the party for a kill, the beast earns XP from the slain NPC's level.
        /// </summary>
        void CombatPointXPDistributed();

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

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        float GetIncubationPercentageById(int itemPropertyId);

        /// <summary>
        /// When a player uses an incubator terminal, the incubator window opens, unless the player lacks the perk or
        /// another player's job is running there.
        /// </summary>
        void UseIncubator();

        /// <summary>
        /// Finishes an incubation job: rolls for a mutation, creates the beast egg with the job's purities and
        /// deletes the job.
        /// </summary>
        /// <param name="job">The incubation job</param>
        /// <param name="player">The player who receives the egg</param>
        void CreateBeastEgg(IncubationJob job, uint player);

        /// <summary>
        /// Creates a beast egg of a beast type on a player.
        /// </summary>
        /// <param name="beastType">The type of beast the egg holds</param>
        /// <param name="player">The player who receives the egg</param>
        /// <returns>The egg</returns>
        uint CreateBeastEgg(BeastType beastType, uint player);

        /// <summary>
        /// Determines if the specified item is an incubation crafting item.
        /// This includes enzymes and DNA but excludes beast eggs.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if used in incubation, false otherwise</returns>
        bool IsIncubationCraftingItem(uint item);

        /// <summary>
        /// Determines if the specified item is a beast egg.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if beast egg, false otherwise</returns>
        bool IsBeastEgg(uint item);

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        void OnRemoveProperty();

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        void UseExtractDNAObject();
    }
}
