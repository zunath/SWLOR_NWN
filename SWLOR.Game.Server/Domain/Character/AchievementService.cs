using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The account achievements: the logic and state that used to be static in <c>Service.Achievement</c>, moved here
    /// unchanged. The event handlers run on the container's instance.
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private readonly IDBService _db;
        private readonly IAchievementNotification _notification;
        private readonly Dictionary<AchievementType, AchievementAttribute> _activeAchievements = new Dictionary<AchievementType, AchievementAttribute>();

        public AchievementService(IDBService db, IAchievementNotification notification)
        {
            _db = db;
            _notification = notification;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void ReserveGuiIds()
        {
            _notification.ReserveIds();
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
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
            _notification.Display(player, achievement.Name);
            PlayerPlugin.PlaySound(player, "gui_prompt", OBJECT_INVALID);
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
