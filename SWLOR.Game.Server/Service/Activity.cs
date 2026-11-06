using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.ActivityService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the "busy" status. The logic lives in <see cref="IActivityService"/>; the event handlers
    /// stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Activity
    {
        private static IActivityService Instance => FacadeAccessor.Resolve<IActivityService>();

        /// <summary>
        /// Marks a target as being busy with a particular type of action.
        /// </summary>
        /// <param name="target">The target to modify.</param>
        /// <param name="type">The type of activity to assign.</param>
        public static void SetBusy(uint target, ActivityStatusType type)
        {
            Instance.SetBusy(target, type);
        }

        /// <summary>
        /// Determines if a target is busy with any type of action.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>true if busy, false otherwise</returns>
        public static bool IsBusy(uint target)
        {
            return Instance.IsBusy(target);
        }

        /// <summary>
        /// Retrieves the type of busy action a target is currently engaged with.
        /// If user isn't busy, ActivityStatusType.Invalid will be returned.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>The type of activity status.</returns>
        public static ActivityStatusType GetBusyType(uint target)
        {
            return Instance.GetBusyType(target);
        }

        /// <summary>
        /// Clears the busy status of a single target.
        /// </summary>
        /// <param name="target">The target whose status will be cleared.</param>
        public static void ClearBusy(uint target)
        {
            Instance.ClearBusy(target);
        }

        /// <summary>
        /// When a player enters the module, wipe their temporary "busy" status.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void WipeStatusOnEntry()
        {
            Instance.WipeStatusOnEntry();
        }

        /// <summary>
        /// When a player dies, wipe their temporary "busy" status.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void WipeStatusOnDeath()
        {
            Instance.WipeStatusOnDeath();
        }
    }
}
