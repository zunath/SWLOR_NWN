using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Each character gets a daily allowance of OOC messages, with more leeway while a DM is present
    /// and no limit in event areas. Every OOC message past the allowance reduces the next RP XP award.
    /// </summary>
    public static class OOCAllowance
    {
        public const int DailyAllowance = 20;
        public const int DMPresentAllowance = 50;
        public const int PenaltyPercentPerMessage = 10;
        public const int MaxPenaltySteps = 100 / PenaltyPercentPerMessage;

        // The allowance day starts at the scheduled daily restart time. It is clock-based so
        // additional restarts during the day do not reset anyone's allowance.
        public static readonly TimeSpan DailyResetTimeUtc = TimeSpan.FromHours(10);

        public static DateTime GetAllowanceDay(DateTime utcNow)
        {
            return (utcNow - DailyResetTimeUtc).Date;
        }

        public static void RecordOOCMessage(RoleplayProgress progress, DateTime utcNow, OOCMessageContext context)
        {
            var allowanceDay = GetAllowanceDay(utcNow);
            if (progress.OOCAllowanceDay != allowanceDay)
            {
                progress.OOCAllowanceDay = allowanceDay;
                progress.DailyOOCMessageCount = 0;
                progress.DailyDMPresentOOCMessageCount = 0;
            }

            progress.OOCMessageCount++;

            if (context == OOCMessageContext.EventArea)
                return;

            if (context == OOCMessageContext.DMPresent && progress.DailyDMPresentOOCMessageCount < DMPresentAllowance)
            {
                progress.DailyDMPresentOOCMessageCount++;
                return;
            }

            progress.DailyOOCMessageCount++;
            if (progress.DailyOOCMessageCount > DailyAllowance)
            {
                progress.PendingOOCPenaltySteps = Math.Min(MaxPenaltySteps, progress.PendingOOCPenaltySteps + 1);
            }
        }

        public static int ApplyPenalty(int xp, int penaltySteps)
        {
            var steps = Math.Clamp(penaltySteps, 0, MaxPenaltySteps);
            return xp * (100 - steps * PenaltyPercentPerMessage) / 100;
        }
    }
}
