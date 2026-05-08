namespace SWLOR.BackgroundServices.Infrastructure
{
    public interface IAppLogger
    {
        void Info(string message);
        void Error(string message);
        void Error(string message, Exception exception);
    }
}
