namespace SWLOR.Core.Plugin
{
    public interface IPluginEntry
    {
        void OnLoaded();
        void OnUnloaded();
    }
}
