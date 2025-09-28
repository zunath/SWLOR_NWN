using SWLOR.Test.Shared.NWScriptMocks;

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
        /// Sets up the mock NWScript service for testing.
        /// This method is thread-safe and idempotent.
        /// </summary>
        protected static void InitializeMockNWScript()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                // Create and set the mock service as the active NWScript implementation
                _mockService = new NWScriptServiceMock();
                SWLOR.NWN.API.Service.NWScript.SetService(_mockService); // Direct call after InternalsVisibleTo
                
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Gets the mock NWScript service instance for direct access to mock data.
        /// This allows tests to verify state changes and set up specific conditions.
        /// </summary>
        protected static NWScriptServiceMock GetMockService()
        {
            InitializeMockNWScript();
            return _mockService ?? throw new InvalidOperationException("Mock service not properly initialized");
        }
    }
}