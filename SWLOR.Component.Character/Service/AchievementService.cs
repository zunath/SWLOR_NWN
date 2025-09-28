using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Character.Service
{
    public class AchievementService : IAchievementService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();
        private static IdReservation _idReservation;
        private static readonly Dictionary<AchievementType, AchievementAttribute> _activeAchievements = new();

        public AchievementService(IDatabaseService db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        public void ReserveGuiIds()
        {
            _idReservation = GuiService.ReserveIds(nameof(AchievementService), 6);
        }

        /// <summary>
        /// When the module caches, read all achievement types and store them into the cache.
        /// </summary>
        public void LoadAchievements()
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
        public void GiveAchievement(uint player, AchievementType achievementType)
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
        public void DisplayAchievementNotificationWindow(uint player, string name)
        {
            const int WindowX = 4;
            const int WindowY = 4;
            const int WindowWidth = 26;

            var centerWindowX = GuiService.CenterStringInWindow(name, WindowX, WindowWidth);
            PostString(player, "Achievement Unlocked", centerWindowX + 2, WindowY+1, ScreenAnchorType.TopRight, 10.0f, GuiStandardColor.ColorWhite, GuiStandardColor.ColorYellow, _idReservation.StartId, GuiTextTexture.TextName);
            PostString(player, " " + name, centerWindowX + 4, WindowY+3, ScreenAnchorType.TopRight, 10.0f, GuiStandardColor.ColorWhite, GuiStandardColor.ColorYellow, _idReservation.StartId + 1, GuiTextTexture.TextName);
            GuiService.DrawWindow(player, _idReservation.StartId + 2, ScreenAnchorType.TopRight, WindowX, WindowY, WindowWidth, 4);
        }

        /// <summary>
        /// Retrieves an achievement detail by its type.
        /// </summary>
        /// <param name="type">The type of achievement to retrieve.</param>
        /// <returns>An achievement detail of the specified type.</returns>
        public AchievementAttribute GetAchievement(AchievementType type)
        {
            return _activeAchievements[type];
        }

        /// <summary>
        /// Retrieves all of the active achievements.
        /// </summary>
        /// <returns>A dictionary containing all of the active achievements.</returns>
        public Dictionary<AchievementType, AchievementAttribute> GetActiveAchievements()
        {
            return _activeAchievements.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
