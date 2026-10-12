using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Each character gets a daily allowance of OOC messages, plus extra leeway during DM events.
    /// Every OOC message past the allowance reduces the next RP XP award.
    /// </summary>
    public static class OOCAllowance
    {
        public const int DailyAllowance = 10;
        public const int DMEventAllowance = 30;
        public const int PenaltyPercentPerMessage = 10;
        public const int MaxPenaltySteps = 100 / PenaltyPercentPerMessage;

        // Allowances reset with the daily server restart.
        public static readonly TimeSpan DailyResetTimeUtc = TimeSpan.FromHours(10);

        public static DateTime GetAllowanceDay(DateTime utcNow)
        {
            return (utcNow - DailyResetTimeUtc).Date;
        }

        public static void RecordOOCMessage(RoleplayProgress progress, DateTime utcNow, bool isDuringDMEvent)
        {
            var allowanceDay = GetAllowanceDay(utcNow);
            if (progress.OOCAllowanceDay != allowanceDay)
            {
                progress.OOCAllowanceDay = allowanceDay;
                progress.DailyOOCMessageCount = 0;
                progress.DailyDMEventOOCMessageCount = 0;
            }

            progress.OOCMessageCount++;

            if (isDuringDMEvent && progress.DailyDMEventOOCMessageCount < DMEventAllowance)
            {
                progress.DailyDMEventOOCMessageCount++;
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
