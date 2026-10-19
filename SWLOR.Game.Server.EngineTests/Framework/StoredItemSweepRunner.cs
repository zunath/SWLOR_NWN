using System.Reflection;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Runs the stored-item sweep (<c>StoredItemSweep</c>) the way a release's server migration does. The sweep is
    /// internal to the game assembly, so it is reached by reflection. Must run inside an engine script.
    /// </summary>
    public static class StoredItemSweepRunner
    {
        /// <summary>The migration version the sweep labels its progress with.</summary>
        private const int LogVersion = 28;

        public static void Run() =>
            typeof(Migration).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredItemSweep")!
                .GetMethod("Migrate", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { LogVersion });
    }
}
