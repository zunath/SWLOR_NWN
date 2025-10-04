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