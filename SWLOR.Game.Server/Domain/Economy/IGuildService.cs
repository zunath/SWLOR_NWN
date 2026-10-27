using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The guilds, a player's guild points and rank, and the guild tasks on offer. The static facade
    /// <c>Service.Guild</c> forwards to the container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IGuildService
    {
        /// <summary>
        /// The highest guild rank, set when the module caches.
        /// </summary>
        int MaxRank { get; }

        /// <summary>
        /// When the guild tasks on offer were chosen, or null before they have been.
        /// </summary>
        DateTime? DateTasksLoaded { get; }

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        void LoadData();

        /// <summary>
        /// Retrieves a guild's detail by the specified type.
        /// </summary>
        /// <param name="guild">The type of guild to retrieve</param>
        /// <returns>A guild detail.</returns>
        GuildAttribute GetGuild(GuildType guild);

        /// <summary>
        /// Works out the guild points a player earns for a base amount, with their rank, perk and social bonuses.
        /// </summary>
        int CalculateGPReward(uint player, GuildType guild, int baseAmount);

        /// <summary>
        /// Gives guild points to a player, ranking them up when they have enough.
        /// </summary>
        void GiveGuildPoints(uint player, GuildType guild, int amount);

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        void RefreshGuildTasks();

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks by rank.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <param name="rank">The rank to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        List<QuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank);

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        Dictionary<string, QuestDetail> GetAllActiveGuildTasks(GuildType guild);

        /// <summary>
        /// Retrieves the GP required to reach next rank.
        /// </summary>
        /// <param name="rank">The rank to search by</param>
        /// <returns>The amount of GP required to reach the next rank</returns>
        int GetGPRequiredForRank(int rank);
    }
}
