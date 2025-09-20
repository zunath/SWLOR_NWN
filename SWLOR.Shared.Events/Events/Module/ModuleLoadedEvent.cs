using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    /// <summary>
    /// Event fired when the module is loaded.
    /// </summary>
    public class ModuleLoadedEvent : BaseEvent
    {
        /// <summary>
        /// The module's last modification time.
        /// </summary>
        public long LastModuleMTime { get; }

        /// <summary>
        /// Whether this is the first time the module has been loaded.
        /// </summary>
        public bool IsFirstLoad { get; }

        public ModuleLoadedEvent(long lastModuleMTime, bool isFirstLoad)
        {
            LastModuleMTime = lastModuleMTime;
            IsFirstLoad = isFirstLoad;
        }
    }
}
