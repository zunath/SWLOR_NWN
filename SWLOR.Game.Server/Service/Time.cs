using System;

namespace SWLOR.Game.Server.Service
{
    public static class Time
    {
        /// <summary>
        /// Returns time in the following manner:
        /// 2 days, 12 hours, 5 minutes, 45 seconds
        /// </summary>
        /// <param name="firstDate">First date to compare</param>
        /// <param name="secondDate">Second date to compare</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
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

        /// <summary>
        /// Returns time in the following manner:
        /// 2D, 12H, 5M, 45S
        /// </summary>
        /// <param name="firstDate">First date to compare</param>
        /// <param name="secondDate">Second date to compare</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
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

        /// <summary>
        /// Returns time in the following manner:
        /// 2 days, 12 hours, 5 minutes, 45 seconds
        /// </summary>
        /// <param name="days">The number of days</param>
        /// <param name="hours">The number of hours</param>
        /// <param name="minutes">The number of minutes</param>
        /// <param name="seconds">The number of seconds</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing a formatted time.</returns>
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

        /// <summary>
        /// Returns time in the following manner:
        /// 2 days, 12 hours, 5 minutes, 45 seconds
        /// </summary>
        /// <param name="time">The time span</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing a formatted time.</returns>
        public static string GetTimeLongIntervals(TimeSpan time, bool showIfZero)
        {
            return GetTimeLongIntervals(time.Days, time.Hours, time.Minutes, time.Seconds, showIfZero);
        }

        /// <summary>
        /// Returns time in the following manner:
        /// 2D, 12H, 5M, 45S
        /// </summary>
        /// <param name="days">The number of days</param>
        /// <param name="hours">The number of hours</param>
        /// <param name="minutes">The number of minutes</param>
        /// <param name="seconds">The number of seconds</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
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
