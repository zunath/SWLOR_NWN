using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the guilds. The logic and state live in <see cref="IGuildService"/>; every member here forwards
    /// to the container's instance, and the event handlers stay here so their names in the handler order manifest do not
    /// change.
    /// </summary>
    public static class Guild
    {
        private static IGuildService Instance => FacadeAccessor.Resolve<IGuildService>();

        public static int MaxRank => Instance.MaxRank;

        public static DateTime? DateTasksLoaded => Instance.DateTasksLoaded;

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadData()
        {
            Instance.LoadData();
        }

        /// <summary>
        /// Retrieves a guild's detail by the specified type.
        /// </summary>
        /// <param name="guild">The type of guild to retrieve</param>
        /// <returns>A guild detail.</returns>
        public static GuildAttribute GetGuild(GuildType guild)
        {
            return Instance.GetGuild(guild);
        }

        public static int CalculateGPReward(uint player, GuildType guild, int baseAmount)
        {
            return Instance.CalculateGPReward(player, guild, baseAmount);
        }

        public static void GiveGuildPoints(uint player, GuildType guild, int amount)
        {
            Instance.GiveGuildPoints(player, guild, amount);
        }

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestsRegistered)]
        public static void RefreshGuildTasks()
        {
            Instance.RefreshGuildTasks();
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks by rank.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <param name="rank">The rank to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public static List<QuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank)
        {
            return Instance.GetActiveGuildTasksByRank(guild, rank);
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public static Dictionary<string, QuestDetail> GetAllActiveGuildTasks(GuildType guild)
        {
            return Instance.GetAllActiveGuildTasks(guild);
        }

        /// <summary>
        /// Retrieves the GP required to reach next rank.
        /// </summary>
        /// <param name="rank">The rank to search by</param>
        /// <returns>The amount of GP required to reach the next rank</returns>
        public static int GetGPRequiredForRank(int rank)
        {
            return Instance.GetGPRequiredForRank(rank);
        }
    }
}
