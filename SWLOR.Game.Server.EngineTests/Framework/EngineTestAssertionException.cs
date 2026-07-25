namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Thrown when an engine test assertion fails. The test is marked as Failed.
    /// </summary>
    public class EngineTestAssertionException : Exception
    {
        public EngineTestAssertionException(string message)
            : base(message)
        {
        }
    }
}
