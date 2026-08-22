namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Marks a public static method as an in-engine integration test.
    /// The method must accept a single EngineTestContext parameter and return Task - and only
    /// Task: synchronous void bodies run outside the cooperative timeout's reach and async
    /// void is unobservable, so the runner rejects both as invalid signatures.
    /// Tests only run when the server is started with SWLOR_ENGINE_TESTS_ENABLED=true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EngineTestAttribute : Attribute
    {
        public string Name { get; }
        public string Category { get; set; }
        public float TimeoutSeconds { get; set; } = 60f;

        public EngineTestAttribute(string name)
        {
            Name = name;
            Category = "General";
        }
    }
}
