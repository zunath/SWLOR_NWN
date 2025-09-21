namespace SWLOR.Shared.Core.Contracts
{
    public interface ITimeService
    {
        /// <summary>
        /// Returns time in the following manner:
        /// 2 days, 12 hours, 5 minutes, 45 seconds
        /// </summary>
        /// <param name="firstDate">First date to compare</param>
        /// <param name="secondDate">Second date to compare</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
        string GetTimeToWaitLongIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero);

        /// <summary>
        /// Returns time in the following manner:
        /// 2D, 12H, 5M, 45S
        /// </summary>
        /// <param name="firstDate">First date to compare</param>
        /// <param name="secondDate">Second date to compare</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
        string GetTimeToWaitShortIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero);

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
        string GetTimeLongIntervals(int days, int hours, int minutes, int seconds, bool showIfZero);

        /// <summary>
        /// Returns time in the following manner:
        /// 2 days, 12 hours, 5 minutes, 45 seconds
        /// </summary>
        /// <param name="time">The time span</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing a formatted time.</returns>
        string GetTimeLongIntervals(TimeSpan time, bool showIfZero);

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
        string GetTimeShortIntervals(int days, int hours, int minutes, int seconds, bool showIfZero);

        /// <summary>
        /// Returns time in the following manner:
        /// 2D, 12H, 5M, 45S
        /// </summary>
        /// <param name="timespan">The timespan</param>
        /// <param name="showIfZero">Will show the units if they are zero.</param>
        /// <returns>String containing the formatted time.</returns>
        string GetTimeShortIntervals(TimeSpan timespan, bool showIfZero);
    }
}
