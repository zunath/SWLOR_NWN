namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Thrown when an engine test cannot run in the current environment. The test is marked as Skipped.
    /// </summary>
    public class EngineTestSkippedException : Exception
    {
        public EngineTestSkippedException(string message)
            : base(message)
        {
        }
    }
}
