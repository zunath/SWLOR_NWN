namespace SWLOR.NWN.API
{
    /// <summary>
    /// Static holder for the script execution provider.
    /// This will be set by the game server during initialization.
    /// </summary>
    public static class ScriptExecutionProvider
    {
        private static IScriptExecutionProvider? _provider;

        /// <summary>
        /// Gets the current script execution provider.
        /// </summary>
        public static IScriptExecutionProvider? Current => _provider;

        /// <summary>
        /// Sets the script execution provider. Should only be called during server initialization.
        /// </summary>
        /// <param name="provider">The provider implementation</param>
        public static void SetProvider(IScriptExecutionProvider provider)
        {
            _provider = provider;
        }
    }
}