namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IScheduler
    {
        IDisposable Schedule(Action task, TimeSpan delay);
        IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay = default);
        void Process();
    }
}