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
        private static NWScriptServiceMock? _mockService;

        /// <summary>
        /// Sets up the mock NWScript service and NWNX plugin mocks for testing.
        /// This method is thread-safe and idempotent.
        /// </summary>
        protected static void InitializeMockNWScript()
        {
            lock (_lock)
            {
                if (_isInitialized) 
                {
                    // Reset mock state to ensure clean test isolation
                    _mockService?.ResetMockState();
                    return;
                }

                _mockService = new NWScriptServiceMock();
                NWScript.SetService(_mockService); 

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

        /// <summary>
        /// Gets the mock NWScript service instance for direct access to mock data.
        /// This allows tests to verify mock state and reset it between tests.
        /// </summary>
        /// <returns>The mock NWScript service instance</returns>
        protected static NWScriptServiceMock GetMockService()
        {
            if (!_isInitialized)
                InitializeMockNWScript();
            
            return _mockService!;
        }

        /// <summary>
        /// Resets all mock state to ensure clean test isolation.
        /// This is automatically called by InitializeMockNWScript() but can be called manually if needed.
        /// </summary>
        protected static void ResetMockState()
        {
            if (_mockService != null)
            {
                _mockService.ResetMockState();
            }
        }
    }
}