using System;


namespace SWLOR.Game.Server.Service
{
    public static class TimeService
    {
        // Returns time in the following manner:
        // 2 days, 12 hours, 5 minutes, 45 seconds
        public static string GetTimeToWaitLongIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero)
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

            return GetTimeLongIntervals(period.Days, period.Hours, period.Minutes, period.Seconds, showIfZero);
        }

        // Returns time in the following manner:
        // 2D, 12H, 5M, 45S
        public static string GetTimeToWaitShortIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero)
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
            
            return GetTimeShortIntervals(period.Days, period.Hours, period.Minutes, period.Seconds, showIfZero);
        }

        // Returns time in the following manner:
        // 2 days, 12 hours, 5 minutes, 45 seconds
        public static string GetTimeLongIntervals(int days, int hours, int minutes, int seconds, bool showIfZero)
        {
            string result = "";

            if (showIfZero || days > 0)
                result += days + (days == 1 ? " day, " : " days, ");
            if (showIfZero || hours > 0)
                result += hours + (hours == 1 ? " hour, " : " hours, ");
            if (showIfZero || minutes > 0)
                result += minutes + (minutes == 1 ? " minute, " : " minutes, ");

            // Always show seconds regardless if showIfZero == false. This is due to milliseconds.
            result += seconds + (seconds == 1 ? " second" : " seconds");

            return result;
        }


        // Returns time in the following manner:
        // 2D, 12H, 5M, 45S
        public static string GetTimeShortIntervals(int days, int hours, int minutes, int seconds, bool showIfZero)
        {
            string result = "";

            if (showIfZero || days > 0)
                result += days + "D, ";

            if (showIfZero || hours > 0)
                result += hours + "H, ";

            if (showIfZero || minutes > 0)
                result += minutes + "M, ";

            // Always show seconds regardless if showIfZero == false. This is due to milliseconds.
            result += seconds + "S";

            return result;
        }

    }
}
