using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the achievements. The logic and state live in <see cref="IAchievementService"/>; every member
    /// here forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Achievement
    {
        private static IAchievementService Instance => FacadeAccessor.Resolve<IAchievementService>();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ReserveGuiIds()
        {
            Instance.ReserveGuiIds();
        }

        /// <summary>
        /// When the module caches, read all achievement types and store them into the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadAchievements()
        {
            Instance.LoadAchievements();
        }

        /// <summary>
        /// Gives an achievement to a player's account (by CD key).
        /// If the player already has this achievement, nothing will happen.
        /// </summary>
        /// <param name="player">The player to give the achievement to.</param>
        /// <param name="achievementType">The achievement to grant.</param>
        public static void GiveAchievement(uint player, AchievementType achievementType)
        {
            Instance.GiveAchievement(player, achievementType);
        }

        /// <summary>
        /// Displays the achievement notification window with the achievement's name and description.
        /// </summary>
        public static void DisplayAchievementNotificationWindow(uint player, string name)
        {
            Instance.DisplayAchievementNotificationWindow(player, name);
        }

        /// <summary>
        /// Retrieves an achievement detail by its type.
        /// </summary>
        /// <param name="type">The type of achievement to retrieve.</param>
        /// <returns>An achievement detail of the specified type.</returns>
        public static AchievementAttribute GetAchievement(AchievementType type)
        {
            return Instance.GetAchievement(type);
        }

        /// <summary>
        /// Retrieves all of the active achievements.
        /// </summary>
        /// <returns>A dictionary containing all of the active achievements.</returns>
        public static Dictionary<AchievementType, AchievementAttribute> GetActiveAchievements()
        {
            return Instance.GetActiveAchievements();
        }
    }
}
