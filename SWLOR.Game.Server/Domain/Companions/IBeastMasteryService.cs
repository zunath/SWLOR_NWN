using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Beast companions: the beast catalog, taming and leveling, the creature event handlers of a spawned beast, and
    /// the incubation items. Spawning a beast is <see cref="IBeastSpawnService"/> and incubating DNA is
    /// <see cref="IBeastIncubationService"/>. Static callers reach the container's instance through
    /// <c>GameServices.BeastMastery</c>; the event handlers are on <see cref="BeastMasteryService"/>.
    /// </summary>
    public interface IBeastMasteryService : IBeastCorpseInfo
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
        /// Writes the stats of a beast's level and purities onto its skin, claw and ability scores. Called when a beast
        /// is spawned and when it gains a level.
        /// </summary>
        /// <param name="beast">The beast creature</param>
        void ApplyStats(uint beast);

        /// <summary>
        /// Picks two different foods at random: the one a beast likes and the one it hates.
        /// </summary>
        (BeastFoodType, BeastFoodType) GetLikedAndHatedFood();

        /// <summary>
        /// When a player's beast is in the party for a kill, the beast earns XP from the slain NPC's level.
        /// </summary>
        void CombatPointXPDistributed();

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        float GetIncubationPercentageById(int itemPropertyId);

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
    }
}
