using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ActivityService;

namespace SWLOR.Game.Server.Service
{
    public static class Activity
    {
        /// <summary>
        /// Marks a target as being busy with a particular type of action.
        /// </summary>
        /// <param name="target">The target to modify.</param>
        /// <param name="type">The type of activity to assign.</param>
        public static void SetBusy(uint target, ActivityStatusType type)
        {
            SetLocalBool(target, "IS_BUSY", true);
            SetLocalInt(target, "BUSY_TYPE", (int)type);
        }

        /// <summary>
        /// Determines if a target is busy with any type of action.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>true if busy, false otherwise</returns>
        public static bool IsBusy(uint target)
        {
            return GetLocalBool(target, "IS_BUSY");
        }

        /// <summary>
        /// Retrieves the type of busy action a target is currently engaged with.
        /// If user isn't busy, ActivityStatusType.Invalid will be returned.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>The type of activity status.</returns>
        public static ActivityStatusType GetBusyType(uint target)
        {
            if (!IsBusy(target))
                return ActivityStatusType.Invalid;

            return (ActivityStatusType) GetLocalInt(target, "BUSY_TYPE");
        }

        /// <summary>
        /// Clears the busy status of a single target.
        /// </summary>
        /// <param name="target">The target whose status will be cleared.</param>
        public static void ClearBusy(uint target)
        {
            DeleteLocalBool(target, "IS_BUSY");
            DeleteLocalInt(target, "BUSY_TYPE");
        }

        /// <summary>
        /// When a player enters the module, wipe their temporary "busy" status.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void WipeStatusOnEntry()
        {
            var player = GetEnteringObject();
            ClearBusy(player);
        }

        /// <summary>
        /// When a player dies, wipe their temporary "busy" status.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void WipeStatusOnDeath()
        {
            var player = GetLastPlayerDied();
            ClearBusy(player);
        }
    }
}
