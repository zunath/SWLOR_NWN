using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The account achievements: the logic and state that used to be static in <c>Service.Achievement</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class AchievementService : IAchievementService
    {
        // The name the GUI id reservation has always been made under (nameof of the facade, Service.Achievement).
        private const string GuiReservationName = "Achievement";

        private readonly IDBService _db;
        private readonly Dictionary<AchievementType, AchievementAttribute> _activeAchievements = new Dictionary<AchievementType, AchievementAttribute>();
        private Gui.IdReservation _idReservation;

        public AchievementService(IDBService db)
        {
            _db = db;
        }

        public void ReserveGuiIds()
        {
            _idReservation = Gui.ReserveIds(GuiReservationName, 6);
        }

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

        public void DisplayAchievementNotificationWindow(uint player, string name)
        {
            const int WindowX = 4;
            const int WindowY = 4;
            const int WindowWidth = 26;

            var centerWindowX = Gui.CenterStringInWindow(name, WindowX, WindowWidth);
            PostString(player, "Achievement Unlocked", centerWindowX + 2, WindowY+1, ScreenAnchor.TopRight, 10.0f, Gui.ColorWhite, Gui.ColorYellow, _idReservation.StartId,Gui.TextName);
            PostString(player, " " + name, centerWindowX + 4, WindowY+3, ScreenAnchor.TopRight, 10.0f, Gui.ColorWhite, Gui.ColorYellow, _idReservation.StartId + 1, Gui.TextName);
            Gui.DrawWindow(player, _idReservation.StartId + 2, ScreenAnchor.TopRight, WindowX, WindowY, WindowWidth, 4);
        }

        public AchievementAttribute GetAchievement(AchievementType type)
        {
            return _activeAchievements[type];
        }

        public Dictionary<AchievementType, AchievementAttribute> GetActiveAchievements()
        {
            return _activeAchievements.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
