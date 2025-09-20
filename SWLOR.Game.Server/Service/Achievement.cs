using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Game.Server.Service
{
    public static class Achievement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static Gui.IdReservation _idReservation;
        private static readonly Dictionary<AchievementType, AchievementAttribute> _activeAchievements = new Dictionary<AchievementType, AchievementAttribute>();

        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void ReserveGuiIds()
        {
            _idReservation = Gui.ReserveIds(nameof(Achievement), 6);
        }

        /// <summary>
        /// When the module caches, read all achievement types and store them into the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadAchievements()
        {
            var achievementTypes = Enum.GetValues(typeof(AchievementType)).Cast<AchievementType>();
            foreach (var achievement in achievementTypes)
            {
                var achievementDetail = achievement.GetAttribute<AchievementType, AchievementAttribute>();

                if (achievementDetail.IsActive)
                {
                    _activeAchievements[achievement] = achievementDetail;
                }
            }
        }

        /// <summary>
        /// Gives an achievement to a player's account (by CD key).
        /// If the player already has this achievement, nothing will happen.
        /// </summary>
        /// <param name="player">The player to give the achievement to.</param>
        /// <param name="achievementType">The achievement to grant.</param>
        public static void GiveAchievement(uint player, AchievementType achievementType)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var cdKey = GetPCPublicCDKey(player);
            var account = _db.Get<Account>(cdKey) ?? new Account(cdKey);
            if (account.Achievements.ContainsKey(achievementType)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var now = DateTime.UtcNow;
            account.Achievements[achievementType] = now;
            _db.Set(account);

            // Player turned off achievement notifications. Nothing left to do here.
            if (!dbPlayer.Settings.DisplayAchievementNotification) return;

            var achievement = _activeAchievements[achievementType];
            DisplayAchievementNotificationWindow(player, achievement.Name);
            PlayerPlugin.PlaySound(player, "gui_prompt", OBJECT_INVALID);
        }

        /// <summary>
        /// Displays the achievement notification window with the achievement's name and description.
        /// </summary>
        public static void DisplayAchievementNotificationWindow(uint player, string name)
        {
            const int WindowX = 4;
            const int WindowY = 4;
            const int WindowWidth = 26;

            var centerWindowX = Gui.CenterStringInWindow(name, WindowX, WindowWidth);
            PostString(player, "Achievement Unlocked", centerWindowX + 2, WindowY+1, ScreenAnchor.TopRight, 10.0f, Gui.ColorWhite, Gui.ColorYellow, _idReservation.StartId,Gui.TextName);
            PostString(player, " " + name, centerWindowX + 4, WindowY+3, ScreenAnchor.TopRight, 10.0f, Gui.ColorWhite, Gui.ColorYellow, _idReservation.StartId + 1, Gui.TextName);
            Gui.DrawWindow(player, _idReservation.StartId + 2, ScreenAnchor.TopRight, WindowX, WindowY, WindowWidth, 4);
        }

        /// <summary>
        /// Retrieves an achievement detail by its type.
        /// </summary>
        /// <param name="type">The type of achievement to retrieve.</param>
        /// <returns>An achievement detail of the specified type.</returns>
        public static AchievementAttribute GetAchievement(AchievementType type)
        {
            return _activeAchievements[type];
        }

        /// <summary>
        /// Retrieves all of the active achievements.
        /// </summary>
        /// <returns>A dictionary containing all of the active achievements.</returns>
        public static Dictionary<AchievementType, AchievementAttribute> GetActiveAchievements()
        {
            return _activeAchievements.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
