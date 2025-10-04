using SWLOR.NWN.API.Service;
using SWLOR.Test.Shared.NWScriptMocks;
using SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks;

namespace SWLOR.Test.Shared
{
    /// <summary>
    /// Base class for all test classes that need to use the mock NWScript implementation.
    /// This ensures that all tests use the mock implementation instead of the real one.
    /// </summary>
    public abstract class TestBase
    {
        private static bool _isInitialized;
        private static readonly Lock _lock = new();

        /// <summary>
        /// Sets up the mock NWScript service and NWNX plugin mocks for testing.
        /// This method is thread-safe and idempotent.
        /// </summary>
        protected static void InitializeMockNWScript()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                NWScript.SetService(new NWScriptServiceMock()); 

                // Initialize all NWNX plugin mocks
                AdministrationPlugin.SetService(new AdministrationPluginMock());
                AreaPlugin.SetService(new AreaPluginMock());
                ChatPlugin.SetService(new ChatPluginMock());
                CreaturePlugin.SetService(new CreaturePluginMock());
                EventsPlugin.SetService(new EventsPluginMock());
                FeatPlugin.SetService(new FeatPluginMock());
                FeedbackPlugin.SetService(new FeedbackPluginMock());
                ItemPlugin.SetService(new ItemPluginMock());
                ItemPropertyPlugin.SetService(new ItemPropertyPluginMock());
                ObjectPlugin.SetService(new ObjectPluginMock());
                PlayerPlugin.SetService(new PlayerPluginMock());
                ProfilerPlugin.SetService(new ProfilerPluginMock());
                UtilPlugin.SetService(new UtilPluginMock());
                VisibilityPlugin.SetService(new VisibilityPluginMock());
                WeaponPlugin.SetService(new WeaponPluginMock());

                _isInitialized = true;
            }
        }
    }
}