using System;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ITimeService
    {
        string GetTimeToWaitLongIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero);
        string GetTimeToWaitShortIntervals(DateTime firstDate, DateTime secondDate, bool showIfZero);
        string GetTimeLongIntervals(int days, int hours, int minutes, int seconds, bool showIfZero);
        string GetTimeShortIntervals(int days, int hours, int minutes, int seconds, bool showIfZero);
    }
}