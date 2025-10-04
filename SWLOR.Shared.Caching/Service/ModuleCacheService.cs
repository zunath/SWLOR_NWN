using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Entity;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Shared.Caching.Service
{
    public class ModuleCacheService : IModuleCacheService
    {
        private readonly IDatabaseService _db;
        private readonly IEventAggregator _eventAggregator;

        public ModuleCacheService(IDatabaseService db, IEventAggregator eventAggregator)
        {
            _db = db;
            _eventAggregator = eventAggregator;
        }

        public void LoadCache()
        {
            var serverConfig = _db.Get<ModuleCache>(ModuleCache.DefaultId) ?? new ModuleCache();

            // Module has changed since last run.
            // Run procedures dependent on the module file changing.
            if (UtilPlugin.GetModuleMTime() != serverConfig.LastModuleMTime)
            {
                Console.WriteLine("Module has changed since last boot. Running module changed event.");

                // DB record must be updated before the event fires, as some
                // events use the server configuration record.
                serverConfig.LastModuleMTime = UtilPlugin.GetModuleMTime();
                _db.Set(serverConfig);

                _eventAggregator.Publish(new OnModuleContentChange(), GetModule());
            }
        }
    }
}
