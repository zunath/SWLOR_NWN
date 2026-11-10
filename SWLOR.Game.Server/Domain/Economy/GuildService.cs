using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _activeGuildTasksByRank = new Dictionary<GuildType, Dictionary<int, List<QuestDetail>>>();
        private readonly Dictionary<GuildType, Dictionary<string, QuestDetail>> _activeGuildTasks = new Dictionary<GuildType, Dictionary<string, QuestDetail>>();

        public GuildService(IDBService db, IPerkService perk, IQuestService quest)
        {
            _db = db;
            _perk = perk;
            _quest = quest;
        }

        public int MaxRank { get; private set; }

        public DateTime? DateTasksLoaded { get; private set; }

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
