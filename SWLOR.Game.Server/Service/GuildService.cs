using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class GuildService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(a => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLoad>(a => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnQuestCompleted>(a => OnQuestCompleted(a.Player, a.QuestID));
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

        /// <summary>
        /// Reward GP to player if the quest awards it.
        /// </summary>
        /// <param name="player">The player who completed the quest.</param>
        /// <param name="questID">The ID of the quest</param>
        private static void OnQuestCompleted(NWPlayer player, int questID)
        {
            var quest = DataService.Get<Quest>(questID);
            // GP rewards not specified. Bail out early.
            if (quest.RewardGuildID == null || quest.RewardGuildPoints <= 0) return;

            GiveGuildPoints(player, (GuildType)quest.RewardGuildID, quest.RewardGuildPoints);
        }

        /// <summary>
        /// Cycle out the available guild tasks if the previous set has been available for 24 hours.
        /// </summary>
        private static void OnModuleLoad()
        {
            var config = DataService.Get<ServerConfiguration>(1);
            var now = DateTime.UtcNow;
            
            // 24 hours haven't passed since the last cycle. Bail out now.
            if (now < config.LastGuildTaskUpdate.AddHours(24)) return;
            
            // Start by marking the existing tasks as not currently offered.
            foreach (var task in DataService.Where<GuildTask>(x => x.IsCurrentlyOffered))
            {
                task.IsCurrentlyOffered = false;
                DataService.SubmitDataChange(task, DatabaseActionType.Update);
            }

            // Active available tasks are grouped by GuildID and RequiredRank. 
            // 10 of each are randomly selected and marked as currently offered.
            // This makes them appear in the dialog menu for players.
            // If there are 10 or less available tasks, all of them will be enabled and no randomization will occur.
            foreach (var guild in DataService.GetAll<Guild>())
            {
                var potentialTasks = DataService.Where<GuildTask>(x => x.GuildID == guild.ID);
                IEnumerable<GuildTask> tasks;

                // Need at least 11 tasks to randomize. We have ten or less. Simply enable all of these.
                if (potentialTasks.Count <= 10)
                {
                    tasks = potentialTasks;
                }
                // Pick 10 tasks randomly out of the potential list.
                else
                {
                    tasks = potentialTasks.OrderBy(o => RandomService.Random()).Take(10);
                }

                // We've got our set of tasks. Mark them as currently offered and submit the data change.
                foreach (var task in tasks)
                {
                    task.IsCurrentlyOffered = true;
                    DataService.SubmitDataChange(task, DatabaseActionType.Update);
                }
            }

            // Update the server config and mark the timestamp.
            config.LastGuildTaskUpdate = now;
            DataService.SubmitDataChange(config, DatabaseActionType.Update);
        }
    }
}
