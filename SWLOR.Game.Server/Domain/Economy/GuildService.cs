using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The guilds: the logic and state that used to be a static class, moved here unchanged. The event
    /// handlers run on the container's instance; static callers reach it through <c>GameServices.Guild</c>.
    /// </summary>
    public class GuildService : IGuildService
    {
        private readonly IDBService _db;
        private readonly IPerkService _perk;
        private readonly IQuestService _quest;
        private readonly Dictionary<GuildType, GuildAttribute> _activeGuilds = new Dictionary<GuildType, GuildAttribute>();
        private readonly Dictionary<int, int> _rankProgression = new Dictionary<int, int>
        {
            // Level, Points Needed
            { 0, 1000 },
            { 1, 5000 },
            { 2, 15000 },
            { 3, 30000 },
            { 4, 45000 },
            { 5, 60000 }
        };
        // The day's guild tasks, picked at random when the quests register: a hot reload keeps the old build's pick and its date
        // (CarryGuildTasks, AdoptGuildTasks), so the task boards and the tasks hidden after completion do not change.
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _activeGuildTasksByRank = new Dictionary<GuildType, Dictionary<int, List<QuestDetail>>>();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _guildTaskRotationByRank = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<GuildType, Dictionary<string, QuestDetail>> _activeGuildTasks = new Dictionary<GuildType, Dictionary<string, QuestDetail>>();

        /// <summary>The key the guild tasks are carried under across a hot reload (<see cref="ReloadCarry"/>).</summary>
        private const string GuildTaskCarryKey = "guildTasks";

        public GuildService(IDBService db, IPerkService perk, IQuestService quest)
        {
            _db = db;
            _perk = perk;
            _quest = quest;
        }

        public int MaxRank { get; private set; }

        [field: ReloadState(ReloadStateKind.Adopted)]
        public DateTime? DateTasksLoaded { get; private set; }

        /// <summary>
        /// Caches the active guilds and the highest rank. A hot reload reruns it; the quests then register again and roll new guild
        /// tasks, which <see cref="AdoptGuildTasks"/> replaces with the old build's.
        /// </summary>
        [BootPhase(BootReload.Rerun, AdoptedBy = nameof(AdoptGuildTasks), CarriedBy = nameof(CarryGuildTasks))]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadData()
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

        public GuildAttribute GetGuild(GuildType guild)
        {
            return _activeGuilds[guild];
        }

        public int CalculateGPReward(uint player, GuildType guild, int baseAmount)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if(!dbPlayer.Guilds.ContainsKey(guild))
                dbPlayer.Guilds[guild] = new PlayerGuild();

            var dbGuild = dbPlayer.Guilds[guild];
            var rankBonus = 0.25f * dbGuild.Rank;
            var perkBonus = _perk.GetPerkLevel(player, PerkType.GuildRelations) * 0.05f;
            var socialBonus = GetAbilityModifier(AbilityType.Social, player) * 0.05f;
            var amount = baseAmount +
                         (perkBonus * baseAmount) +
                         (rankBonus * baseAmount) +
                         (socialBonus * baseAmount);

            return (int)amount;
        }

        public void GiveGuildPoints(uint player, GuildType guild, int amount)
        {
            if (amount <= 0) return;

            // Clamp max GP baseAmount
            if (amount > 1000)
                amount = 1000;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

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
            _db.Set(dbPlayer);
        }

        [NWNEventHandler(ScriptName.OnQuestsRegistered)]
        public void RefreshGuildTasks()
        {
            if (DateTasksLoaded != null) return;

            for (var rank = 0; rank < MaxRank; rank++)
            {
                foreach (var (type, _) in _activeGuilds)
                {
                    var potentialTasks = _quest.GetQuestsByGuild(type, rank);
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
        /// When a hot reload quiesces this build: leaves the guild tasks it picked (by quest id, per guild and rank) and the time it
        /// picked them on the module for the next build (<see cref="AdoptGuildTasks"/>). It changes nothing.
        /// </summary>
        public void CarryGuildTasks()
        {
            if (DateTasksLoaded == null)
                return;

            var carry = new JObject
            {
                ["loaded"] = DateTasksLoaded.Value,
                ["tasks"] = new JArray(_activeGuildTasksByRank.SelectMany(guild => guild.Value.Select(rank => new JObject
                {
                    ["guild"] = (int)guild.Key,
                    ["rank"] = rank.Key,
                    ["quests"] = new JArray(rank.Value.Select(quest => quest.QuestId)),
                    ["rotation"] = new JArray(_guildTaskRotationByRank[guild.Key][rank.Key].Select(quest => quest.QuestId)),
                }))),
            };

            ReloadCarry.Put(GuildTaskCarryKey, carry.ToString(Formatting.None));
        }

        /// <summary>
        /// On a hot reload, after the rerun handlers (the quests registered again and rolled new tasks): puts back the tasks the old
        /// build picked and the time it picked them, so the task boards and the tasks hidden after completion stay as they were. A
        /// quest this build no longer has is left out. Without carried tasks the newly rolled ones stay.
        /// </summary>
        public void AdoptGuildTasks()
        {
            var text = ReloadCarry.Take(GuildTaskCarryKey);
            if (text == null)
                return;

            var carry = JObject.Parse(text);
            _activeGuildTasks.Clear();
            _activeGuildTasksByRank.Clear();
            foreach (var entry in carry["tasks"])
            {
                var guild = (GuildType)entry["guild"].Value<int>();
                var tasks = entry["quests"].Values<string>()
                    .Select(questId => _quest.GetQuestByIdOrDefault(questId))
                    .Where(quest => quest != null)
                    .ToList();

                if (!_activeGuildTasks.ContainsKey(guild))
                    _activeGuildTasks[guild] = new Dictionary<string, QuestDetail>();
                if (!_activeGuildTasksByRank.ContainsKey(guild))
                    _activeGuildTasksByRank[guild] = new Dictionary<int, List<QuestDetail>>();

                foreach (var task in tasks)
                    _activeGuildTasks[guild][task.QuestId] = task;
                var rank = entry["rank"].Value<int>();
                _activeGuildTasksByRank[guild][rank] = tasks;
                if (!_guildTaskRotationByRank.TryGetValue(guild, out var rotations))
                    _guildTaskRotationByRank[guild] = rotations = new();
                var rolled = rotations.TryGetValue(rank, out var pool) ? pool : new List<QuestDetail>();
                var ids = entry["rotation"]?.Values<string>() ?? tasks.Select(task => task.QuestId);
                rotations[rank] = ids.Concat(rolled.Select(task => task.QuestId)).Distinct()
                    .Select(id => _quest.GetQuestByIdOrDefault(id)).Where(task => task != null).ToList();
            }

            DateTasksLoaded = carry["loaded"].ToObject<DateTime>();
        }

        public Dictionary<string, QuestDetail> GetGuildTasksForPlayer(GuildType guild, IReadOnlyDictionary<string, PlayerQuest> playerQuests)
        {
            if (!guild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion ||
                DateTasksLoaded == null ||
                !_guildTaskRotationByRank.ContainsKey(guild))
                return GetAllActiveGuildTasks(guild);

            var result = new Dictionary<string, QuestDetail>();
            foreach (var (rank, active) in _activeGuildTasksByRank[guild])
            {
                var rotation = _guildTaskRotationByRank[guild][rank];
                var tasksById = rotation.ToDictionary(x => x.QuestId, x => x);
                var boardSize = active.Count;
                var questIds = SelectPlayerTaskIds(rotation.Select(x => x.QuestId).ToList(), boardSize, playerQuests, DateTasksLoaded.Value);

                foreach (var questId in questIds)
                {
                    result[questId] = tasksById[questId];
                }
            }

            return result;
        }

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

        public bool IsCompletedTaskHidden(GuildType guild, DateTime? dateLastCompleted, DateTime? dateTasksLoaded)
        {
            if (guild.GetAttribute<GuildType, GuildAttribute>().TasksRefreshAfterCompletion)
                return false;

            return dateLastCompleted >= dateTasksLoaded;
        }

        public List<QuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank)
        {
            if(!_activeGuildTasksByRank.ContainsKey(guild))
                return new List<QuestDetail>();

            return _activeGuildTasksByRank[guild][rank].ToList();
        }

        public Dictionary<string, QuestDetail> GetAllActiveGuildTasks(GuildType guild)
        {
            if(!_activeGuildTasks.ContainsKey(guild))
                return new Dictionary<string, QuestDetail>();

            return _activeGuildTasks[guild].ToDictionary(x => x.Key, y => y.Value);
        }

        public int GetGPRequiredForRank(int rank)
        {
            return _rankProgression[rank];
        }
    }
}
