using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Each character gets a daily allowance of OOC messages, with extra allowances in event areas
    /// and while a DM is present. Every OOC message past the allowances reduces the next RP XP award.
    /// The limits are configured in swlor.env (see ApplicationSettings); these are the fallbacks.
    /// </summary>
    public static class OOCAllowance
    {
        public const int DefaultDailyAllowance = 20;
        public const int DefaultDMPresentAllowance = 50;
        public static readonly int? DefaultEventAreaAllowance = null; // null = unlimited
        public const int DefaultPenaltyPercentPerMessage = 10;
        public const int MaxPenaltySteps = 100;

        // The allowance day starts at the scheduled daily restart time. It is clock-based so
        // additional restarts during the day do not reset anyone's allowance.
        public static readonly TimeSpan DailyResetTimeUtc = TimeSpan.FromHours(10);

        public static DateTime GetAllowanceDay(DateTime utcNow)
        {
            return (utcNow - DailyResetTimeUtc).Date;
        }

        /// <summary>
        /// Records an OOC message. Event-area allowance is used first, then DM-present allowance,
        /// then the daily allowance. A null event-area allowance means OOC in event areas is unlimited.
        /// </summary>
        public static void RecordOOCMessage(
            RoleplayProgress progress,
            DateTime utcNow,
            OOCMessageContext context,
            int dailyAllowance,
            int dmPresentAllowance,
            int? eventAreaAllowance)
        {
            var allowanceDay = GetAllowanceDay(utcNow);
            if (progress.OOCAllowanceDay != allowanceDay)
            {
                progress.OOCAllowanceDay = allowanceDay;
                progress.DailyOOCMessageCount = 0;
                progress.DailyDMPresentOOCMessageCount = 0;
                progress.DailyEventAreaOOCMessageCount = 0;
            }

            progress.OOCMessageCount++;

            if (context.HasFlag(OOCMessageContext.EventArea))
            {
                if (eventAreaAllowance == null)
                    return;

                if (progress.DailyEventAreaOOCMessageCount < eventAreaAllowance)
                {
                    progress.DailyEventAreaOOCMessageCount++;
                    return;
                }
            }

            if (context.HasFlag(OOCMessageContext.DMPresent) && progress.DailyDMPresentOOCMessageCount < dmPresentAllowance)
            {
                progress.DailyDMPresentOOCMessageCount++;
                return;
            }

            progress.DailyOOCMessageCount++;
            if (progress.DailyOOCMessageCount > dailyAllowance)
            {
                progress.PendingOOCPenaltySteps = Math.Min(MaxPenaltySteps, progress.PendingOOCPenaltySteps + 1);
            }
        }

        public static int ApplyPenalty(int xp, int penaltySteps, int penaltyPercentPerMessage)
        {
            var reductionPercent = Math.Clamp(penaltySteps * penaltyPercentPerMessage, 0, 100);
            return xp * (100 - reductionPercent) / 100;
        }
    }
}
