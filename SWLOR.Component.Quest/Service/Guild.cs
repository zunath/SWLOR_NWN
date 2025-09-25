using SWLOR.Component.Quest.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Quest.Service
{
    public class GuildService : IGuildService
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly Dictionary<GuildType, GuildAttribute> _activeGuilds = new();
        private readonly Dictionary<int, int> _rankProgression = new()
        {
            // Level, Points Needed
            { 0, 1000 },
            { 1, 5000 },
            { 2, 15000 },
            { 3, 30000 },
            { 4, 45000 },
            { 5, 60000 }
        };

        public GuildService(IDatabaseService db, IRandomService random, IPerkService perkService)
        {
            _db = db;
            _random = random;
            _perkService = perkService;
        }

        public int MaxRank { get; private set; }

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
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

        /// <summary>
        /// Retrieves a guild's detail by the specified type.
        /// </summary>
        /// <param name="guild">The type of guild to retrieve</param>
        /// <returns>A guild detail.</returns>
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
            var perkBonus = _perkService.GetPerkLevel(player, PerkType.GuildRelations) * 0.05f;
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


        /// <summary>
        /// Retrieves the GP required to reach next rank.
        /// </summary>
        /// <param name="rank">The rank to search by</param>
        /// <returns>The amount of GP required to reach the next rank</returns>
        public int GetGPRequiredForRank(int rank)
        {
            return _rankProgression[rank];
        }

    }
}
