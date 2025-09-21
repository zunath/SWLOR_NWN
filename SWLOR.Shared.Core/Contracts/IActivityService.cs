using SWLOR.Game.Server.Service.ActivityService;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IActivityService
    {
        /// <summary>
        /// Marks a target as being busy with a particular type of action.
        /// </summary>
        /// <param name="target">The target to modify.</param>
        /// <param name="type">The type of activity to assign.</param>
        void SetBusy(uint target, ActivityStatusType type);

        /// <summary>
        /// Determines if a target is busy with any type of action.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>true if busy, false otherwise</returns>
        bool IsBusy(uint target);

        /// <summary>
        /// Retrieves the type of busy action a target is currently engaged with.
        /// If user isn't busy, ActivityStatusType.Invalid will be returned.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>The type of activity status.</returns>
        ActivityStatusType GetBusyType(uint target);

        /// <summary>
        /// Clears the busy status of a single target.
        /// </summary>
        /// <param name="target">The target whose status will be cleared.</param>
        void ClearBusy(uint target);
    }
}
