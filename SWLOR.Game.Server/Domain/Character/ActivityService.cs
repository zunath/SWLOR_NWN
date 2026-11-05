using SWLOR.Game.Server.Service.ActivityService;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The "busy" status of players and creatures: the logic that used to be static in <c>Service.Activity</c>,
    /// moved here unchanged. The facade keeps the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class ActivityService : IActivityService
    {
        public void SetBusy(uint target, ActivityStatusType type)
        {
            SetLocalBool(target, "IS_BUSY", true);
            SetLocalInt(target, "BUSY_TYPE", (int)type);
        }

        public bool IsBusy(uint target)
        {
            return GetLocalBool(target, "IS_BUSY");
        }

        public ActivityStatusType GetBusyType(uint target)
        {
            if (!IsBusy(target))
                return ActivityStatusType.Invalid;

            return (ActivityStatusType) GetLocalInt(target, "BUSY_TYPE");
        }

        public void ClearBusy(uint target)
        {
            DeleteLocalBool(target, "IS_BUSY");
            DeleteLocalInt(target, "BUSY_TYPE");
        }

        public void WipeStatusOnEntry()
        {
            var player = GetEnteringObject();
            ClearBusy(player);
        }

        public void WipeStatusOnDeath()
        {
            var player = GetLastPlayerDied();
            ClearBusy(player);
        }
    }
}
