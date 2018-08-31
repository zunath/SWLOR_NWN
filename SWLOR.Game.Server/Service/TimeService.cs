using System;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class TimeService : ITimeService
    {
        // Returns time in the following manner:
        // 2 days, 12 hours, 5 minutes, 45 seconds
        public string GetTimeToWaitLongIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero)
        {
            TimeSpan period;

            if (firstDate > secondDate)
            {
                period = firstDate - secondDate;
            }
            else
            {
                period = secondDate - firstDate;
            }
            
            string result = "";

            if (showIfZero || period.Days > 0)
                result += period.Days + (period.Days == 1 ? " day, " : " days, ");
            if (showIfZero || period.Hours > 0)
                result += period.Hours + (period.Hours == 1 ? " hour, " : " hours, ");
            if (showIfZero || period.Minutes > 0)
                result += period.Minutes + (period.Minutes == 1 ? " minute, " : " minutes, ");

            // Always show seconds regardless if showIfZero == false. This is due to milliseconds.
            result += period.Seconds + (period.Seconds == 1 ? " second" : " seconds");

            return result;
        }

        // Returns time in the following manner:
        // 2D, 12H, 5M, 45S
        public string GetTimeToWaitShortIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero)
        {
            TimeSpan period;

            if (firstDate > secondDate)
            {
                period = firstDate - secondDate;
            }
            else
            {
                period = secondDate - firstDate;
            }

            string result = "";

            if (showIfZero || period.Days > 0)
                result += period.Days + "D, ";

            if (showIfZero || period.Hours > 0)
                result += period.Hours + "H, ";

            if (showIfZero || period.Minutes > 0)
                result += period.Minutes + "M, ";

            // Always show seconds regardless if showIfZero == false. This is due to milliseconds.
            result += period.Seconds + "S";

            return result;
        }
    }
}
