using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class GuildService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(a => OnModuleEnter());
        }

        /// <summary>
        /// Handles adding any missing PCGuildPoint records for a player to the database.
        /// </summary>
        private static void OnModuleEnter()
        {
            NWPlayer player = GetEnteringObject();
            if (!player.IsPlayer) return;

            // If player is missing any entries for guild points, add them now.
            foreach (var guild in DataService.GetAll<Guild>())
            {
                var pcGP = DataService.SingleOrDefault<PCGuildPoint>(x => x.GuildID == guild.ID && x.PlayerID == player.GlobalID);

                // No GP entry found. Add one now.
                if (pcGP == null)
                {
                    pcGP = new PCGuildPoint
                    {
                        GuildID = guild.ID,
                        PlayerID = player.GlobalID,
                        Points = 0,
                        Rank = 0
                    };

                    DataService.SubmitDataChange(pcGP, DatabaseActionType.Insert);
                }
            }
        }

        private static Dictionary<int, int> _rankProgression;

        /// <summary>
        /// This dictionary tracks the GP required for a player to increase his/her rank in a guild.
        /// All guilds use the same GP progression.
        /// </summary>
        public static Dictionary<int, int> RankProgression
        {
            get
            {
                if (_rankProgression == null)
                {
                    _rankProgression = new Dictionary<int, int>
                    {
                        // Level, Points Needed
                        { 0, 1000 },
                        { 1, 5000 },
                        { 2, 10000 },
                        { 3, 15000 },
                        { 4, 20000 },
                        { 5, 25000 }
                    };
                }

                return _rankProgression;
            }
        }

        /// <summary>
        /// Gives GP to a player for a given guild.
        /// If the amount is less than 1, nothing will happen.
        /// If the amount is greater than 1000, the amount will be set to 1000.
        /// If the player ranks up, a message will be sent to him/her and an OnPlayerGuildRankUp event will be published.
        /// </summary>
        /// <param name="player">The player to give GP.</param>
        /// <param name="guild">The guild this GP will apply to.</param>
        /// <param name="amount">The amount of GP to grant.</param>
        public static void GiveGuildPoints(NWPlayer player, GuildType guild, int amount)
        {
            if (amount <= 0) return;

            // Clamp max GP amount
            if (amount > 1000)
                amount = 1000;

            var dbGuild = DataService.Get<Guild>((int) guild);
            var pcGP = DataService.Single<PCGuildPoint>(x => x.GuildID == (int) guild && x.PlayerID == player.GlobalID);
            pcGP.Points += amount;

            // Clamp player GP to the highest rank.
            int maxRank = RankProgression.Keys.Max();
            int maxGP = RankProgression[maxRank];
            if (pcGP.Points >= maxGP)
                pcGP.Points = maxGP-1;

            // Are we able to rank up?
            bool didRankUp = false;
            if (pcGP.Rank < maxRank)
            {
                // Is it time for a rank up?
                int nextRank = RankProgression[pcGP.Rank];
                if (pcGP.Points >= nextRank)
                {
                    // Let's do a rank up.
                    pcGP.Rank++;
                    player.SendMessage(ColorTokenService.Green("You've reached rank " + pcGP.Rank + " in the " + dbGuild.Name + "!"));
                    didRankUp = true;
                }
            }

            // Submit changes to the DB/cache.
            DataService.SubmitDataChange(pcGP, DatabaseActionType.Update);

            // If the player ranked up, publish an event saying so.
            if (didRankUp)
            {
                MessageHub.Instance.Publish(new OnPlayerGuildRankUp(player.GlobalID, pcGP.Rank));
            }
        }
    }
}
