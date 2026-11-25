using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Guild
    {
        private static readonly Dictionary<GuildType, GuildAttribute> _activeGuilds = new();
        private static readonly Dictionary<int, int> _rankProgression = new()
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
        private static readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _activeGuildTasksByRank = new();
        private static readonly Dictionary<GuildType, Dictionary<string, QuestDetail>> _activeGuildTasks = new();
        private static readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _guildTaskRotationByRank = new();

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
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
            var perkBonus = Perk.GetPerkLevel(player, PerkType.GuildRelations) * 0.05f;
            var socialBonus = GetAbilityModifier(AbilityType.Social, player) * 0.05f;
            var amount = baseAmount +
                         (perkBonus * baseAmount) +
                         (rankBonus * baseAmount) +
                         (socialBonus * baseAmount);

            return (int)amount;
        }

        public static void GiveGuildPoints(uint player, GuildType guild, int amount)
        {
            if (amount <= 0) return;

            // Clamp max GP baseAmount
            if (amount > 1000)
                amount = 1000;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Guilds.ContainsKey(guild))
                dbPlayer.Guilds[guild] = new PlayerGuild();

            var dbGuild = dbPlayer.Guilds[guild];
            dbGuild.Points += amount;

            // Clamp player GP to the highest rank.
            var maxGP = _rankProgression[MaxRank];
            if (dbGuild.Points >= maxGP)
                dbGuild.Points = maxGP - 1;

            var detail = _activeGuilds[guild];
            SendMessageToPC(player, $"You earned {amount} {detail.Name} guild points");

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
            DB.Set(dbPlayer);
        }

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestsRegistered)]
        public static void RefreshGuildTasks()
        {
            if (DateTasksLoaded != null) return;

            for (var rank = 0; rank < MaxRank; rank++)
            {
                foreach (var (type, _) in _activeGuilds)
                {
                    var potentialTasks = Quest.GetQuestsByGuild(type, rank);
                    List<QuestDetail> rotation;

                    // Need at least 11 tasks to randomize. We have ten or less. Simply enable all of these.
                    if (potentialTasks.Count <= 10)
                    {
                        rotation = potentialTasks;
                    }
                    // Shuffle the whole list. The first 10 go on the board; on a guild marked TasksRefreshAfterCompletion
                    // the rest replace the tasks a player completes.
                    else
                    {
                        rotation = potentialTasks
                            .OrderBy(o => Random.Next())
                            .ToList();
                    }

                    var tasks = rotation.Take(10).ToList();

                    if(!_activeGuildTasks.ContainsKey(type))
                        _activeGuildTasks[type] = new Dictionary<string, QuestDetail>();

                    if(!_activeGuildTasksByRank.ContainsKey(type))
                        _activeGuildTasksByRank[type] = new Dictionary<int, List<QuestDetail>>();

                    foreach (var task in tasks)
                    {
                        _activeGuildTasks[type][task.QuestId] = task;
                    }

                    _activeGuildTasksByRank[type][rank] = tasks;

                    if (!_guildTaskRotationByRank.ContainsKey(type))
                        _guildTaskRotationByRank[type] = new Dictionary<int, List<QuestDetail>>();

                    _guildTaskRotationByRank[type][rank] = rotation;
                }
            }

            DateTasksLoaded = DateTime.UtcNow;
        }

        /// <summary>
        /// Whether a guild's board hides a task the player completed since the tasks were loaded. Most guilds keep a completed
        /// task off the board until the next server start. A guild marked <see cref="GuildAttribute.TasksRefreshAfterCompletion"/>
        /// does not hide it here, because <see cref="GetGuildTasksForPlayer"/> already swaps it for a different task of the same rank.
        /// </summary>
        /// <param name="guild">The guild whose board is being built.</param>
        /// <param name="dateLastCompleted">When the player last completed the task, or null if they have not.</param>
        /// <param name="dateTasksLoaded">When the guild's tasks were loaded, or null before they are.</param>
        public static bool IsCompletedTaskHidden(GuildType guild, DateTime? dateLastCompleted, DateTime? dateTasksLoaded)
        {
            if (guild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion)
                return false;

            return dateLastCompleted >= dateTasksLoaded;
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
        /// Retrieves the tasks a guild's board offers this player. Most guilds offer the shared list from <see cref="GetAllActiveGuildTasks"/>.
        /// A guild marked <see cref="GuildAttribute.TasksRefreshAfterCompletion"/> replaces each task the player completed since the
        /// tasks were loaded with a different task of the same rank (<see cref="SelectPlayerTaskIds"/>).
        /// </summary>
        /// <param name="guild">The guild whose board is being built.</param>
        /// <param name="playerQuests">The player's quests.</param>
        /// <returns>The tasks keyed by quest ID, in board order.</returns>
        public static Dictionary<string, QuestDetail> GetGuildTasksForPlayer(GuildType guild, IReadOnlyDictionary<string, PlayerQuest> playerQuests)
        {
            if (!guild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion ||
                DateTasksLoaded == null ||
                !_guildTaskRotationByRank.ContainsKey(guild))
                return GetAllActiveGuildTasks(guild);

            var result = new Dictionary<string, QuestDetail>();
            foreach (var (rank, rotation) in _guildTaskRotationByRank[guild])
            {
                var tasksById = rotation.ToDictionary(x => x.QuestId, x => x);
                var boardSize = _activeGuildTasksByRank[guild][rank].Count;
                var questIds = SelectPlayerTaskIds(rotation.Select(x => x.QuestId).ToList(), boardSize, playerQuests, DateTasksLoaded.Value);

                foreach (var questId in questIds)
                {
                    result[questId] = tasksById[questId];
                }
            }

            return result;
        }

        /// <summary>
        /// Picks the tasks of one guild rank a player is offered when completed tasks rotate out. Every task the player has in
        /// progress stays on the board. The rest of the board is filled in rotation order with tasks the player has not completed
        /// since the tasks were loaded, so a completed task gives way to the next one in the rotation. Once the player has completed
        /// every task of the rank, the tasks they completed longest ago come back, so the task they just completed does not return
        /// straight away while the rank has more tasks than the board holds.
        /// </summary>
        /// <param name="rotation">Every task of the guild rank, in rotation order. The first <paramref name="boardSize"/> are the shared board.</param>
        /// <param name="boardSize">How many tasks the board offers.</param>
        /// <param name="playerQuests">The player's quests.</param>
        /// <param name="dateTasksLoaded">When the guild's tasks were loaded.</param>
        /// <returns>The quest IDs to offer: the in-progress and not yet completed tasks in rotation order, then any that came back, oldest first.</returns>
        public static List<string> SelectPlayerTaskIds(
            IReadOnlyList<string> rotation,
            int boardSize,
            IReadOnlyDictionary<string, PlayerQuest> playerQuests,
            DateTime dateTasksLoaded)
        {
            var selected = new HashSet<string>();

            bool IsInProgress(string questId) =>
                playerQuests.TryGetValue(questId, out var quest) && quest.DateLastCompleted == null;

            bool IsCompletedSinceLoad(string questId) =>
                playerQuests.TryGetValue(questId, out var quest) && quest.DateLastCompleted >= dateTasksLoaded;

            foreach (var questId in rotation.Where(IsInProgress))
                selected.Add(questId);

            foreach (var questId in rotation.Where(x => !IsCompletedSinceLoad(x)))
            {
                if (selected.Count >= boardSize)
                    break;

                selected.Add(questId);
            }

            var board = rotation.Where(selected.Contains).ToList();

            var completedOldestFirst = rotation
                .Where(IsCompletedSinceLoad)
                .OrderBy(x => playerQuests[x].DateLastCompleted)
                .ToList();
            foreach (var questId in completedOldestFirst)
            {
                if (board.Count >= boardSize)
                    break;

                board.Add(questId);
            }

            return board;
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
