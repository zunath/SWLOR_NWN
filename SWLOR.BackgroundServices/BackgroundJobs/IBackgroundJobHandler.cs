namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public interface IBackgroundJobHandler
    {
        Task HandleAsync(string payload, CancellationToken cancellationToken);
    }
}
