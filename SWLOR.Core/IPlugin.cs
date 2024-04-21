namespace SWLOR.Core
{
    public interface IPlugin
    {
        void OnStart();
        void OnShutdown();
    }
}
