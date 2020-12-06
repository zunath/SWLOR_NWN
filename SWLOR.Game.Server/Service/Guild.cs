using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.QuestService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Guild
    {
        private static readonly Dictionary<GuildType, GuildAttribute> _activeGuilds = new Dictionary<GuildType, GuildAttribute>();
        private static readonly Dictionary<int, int> _rankProgression = new Dictionary<int, int>
        {
            // Level, Points Needed
            { 0, 1000 },
            { 1, 5000 },
            { 2, 15000 },
            { 3, 30000 },
            { 4, 45000 },
            { 5, 60000 }
        };

        public static int MaxRank { get; private set; }
        public static DateTime? DateTasksLoaded { get; private set; }
        private static readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _activeGuildTasksByRank = new Dictionary<GuildType, Dictionary<int, List<QuestDetail>>>();
        private static readonly Dictionary<GuildType, Dictionary<string, QuestDetail>> _activeGuildTasks = new Dictionary<GuildType, Dictionary<string, QuestDetail>>();

        /// <summary>
        /// When the module loads, cache relevant data and load guild tasks.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadData()
        {
            var guildTypes = Enum.GetValues(typeof(GuildType)).Cast<GuildType>();
            foreach (var guildType in guildTypes)
            {
                var detail = guildType.GetAttribute<GuildType, GuildAttribute>();

                if (detail.IsActive)
                {
                    _activeGuilds[guildType] = detail;
                }
            }

            MaxRank = _rankProgression.Keys.Max();

            RefreshGuildTasks();
        }

        /// <summary>
        /// Retrieves a guild's detail by the specified type.
        /// </summary>
        /// <param name="guild">The type of guild to retrieve</param>
        /// <returns>A guild detail.</returns>
        public static GuildAttribute GetGuild(GuildType guild)
        {
            return _activeGuilds[guild];
        }

        public static int CalculateGPReward(uint player, GuildType guild, int baseAmount)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if(!dbPlayer.Guilds.ContainsKey(guild))
                dbPlayer.Guilds[guild] = new PlayerGuild();

            var dbGuild = dbPlayer.Guilds[guild];
            var rankBonus = 0.25f * dbGuild.Rank;
            var perkBonus = Perk.GetEffectivePerkLevel(player, PerkType.GuildRelations);
            var amount = baseAmount + perkBonus * baseAmount;

            return amount + (int) (amount * rankBonus);
        }

        public static void GiveGuildPoints(uint player, GuildType guild, int baseAmount)
        {
            if (baseAmount <= 0) return;

            // Clamp max GP baseAmount
            if (baseAmount > 1000)
                baseAmount = 1000;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Guilds.ContainsKey(guild))
                dbPlayer.Guilds[guild] = new PlayerGuild();

            var dbGuild = dbPlayer.Guilds[guild];
            dbGuild.Points += baseAmount;

            // Clamp player GP to the highest rank.
            var maxGP = _rankProgression[MaxRank];
            if (dbGuild.Points >= maxGP)
                dbGuild.Points = maxGP - 1;

            var detail = _activeGuilds[guild];
            SendMessageToPC(player, $"You earned {baseAmount} {detail.Name} guild points");

            // Are we able to rank up?
            if (dbGuild.Rank < MaxRank)
            {
                // Is it time for a rank up?
                var nextRank = _rankProgression[dbGuild.Rank];
                if (dbGuild.Points >= nextRank)
                {
                    // Let's do a rank up.
                    dbGuild.Rank++;
                    SendMessageToPC(player, ColorToken.Green("You've reached rank " + dbGuild.Rank + " in the " + detail.Name + "!"));
                }
            }

            dbPlayer.Guilds[guild] = dbGuild;
            DB.Set(playerId, dbPlayer);
        }

        private static void RefreshGuildTasks()
        {
            if (DateTasksLoaded != null) return;

            for (var rank = 0; rank < MaxRank; rank++)
            {
                foreach (var (type, _) in _activeGuilds)
                {
                    var potentialTasks = Quest.GetQuestsByGuild(type, rank);
                    List<QuestDetail> tasks;

                    // Need at least 11 tasks to randomize. We have ten or less. Simply enable all of these.
                    if (potentialTasks.Count <= 10)
                    {
                        tasks = potentialTasks;
                    }
                    // Pick 10 tasks randomly out of the potential list.
                    else
                    {
                        tasks = potentialTasks
                            .OrderBy(o => Random.Next())
                            .Take(10)
                            .ToList();
                    }

                    if(!_activeGuildTasks.ContainsKey(type))
                        _activeGuildTasks[type] = new Dictionary<string, QuestDetail>();

                    if(!_activeGuildTasksByRank.ContainsKey(type))
                        _activeGuildTasksByRank[type] = new Dictionary<int, List<QuestDetail>>();

                    foreach (var task in tasks)
                    {
                        _activeGuildTasks[type][task.QuestId] = task;
                    }

                    _activeGuildTasksByRank[type][rank] = tasks;
                }
            }

            DateTasksLoaded = DateTime.UtcNow;
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks by rank.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <param name="rank">The rank to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public static List<QuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank)
        {
            if(!_activeGuildTasksByRank.ContainsKey(guild))
                return new List<QuestDetail>();

            return _activeGuildTasksByRank[guild][rank].ToList();
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public static Dictionary<string, QuestDetail> GetAllActiveGuildTasks(GuildType guild)
        {
            if(!_activeGuildTasks.ContainsKey(guild))
                return new Dictionary<string, QuestDetail>();

            return _activeGuildTasks[guild].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves the GP required to reach next rank.
        /// </summary>
        /// <param name="rank">The rank to search by</param>
        /// <returns>The amount of GP required to reach the next rank</returns>
        public static int GetGPRequiredForRank(int rank)
        {
            return _rankProgression[rank];
        }
    }
}
