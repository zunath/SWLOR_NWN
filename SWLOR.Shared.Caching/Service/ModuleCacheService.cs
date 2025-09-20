using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Entity;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Infrastructure;

namespace SWLOR.Shared.Caching.Service
{
    internal class ModuleCacheService
    {
        private readonly IDatabaseService _db;

        public ModuleCacheService(IDatabaseService db)
        {
            _db = db;
        }

        [ScriptHandler<OnEventsHooked>()]
        public void OnDatabaseLoaded()
        {
            var serverConfig = _db.Get<ModuleCache>(ModuleCache.DefaultId);

            // Module has changed since last run.
            // Run procedures dependent on the module file changing.
            if (UtilPlugin.GetModuleMTime() != serverConfig.LastModuleMTime)
            {
                Console.WriteLine("Module has changed since last boot. Running module changed event.");

                // DB record must be updated before the event fires, as some
                // events use the server configuration record.
                serverConfig.LastModuleMTime = UtilPlugin.GetModuleMTime();
                _db.Set(serverConfig);

                ExecuteScript(ScriptName.OnModuleContentChange, GetModule());
            }

            // Fire off the mod_cache event which is used for caching data, before mod_load runs.
            ExecuteScript(ScriptName.OnModuleCacheBefore, GetModule());
            ExecuteScript(ScriptName.OnModuleCacheAfter, GetModule());
        }
    }
}
