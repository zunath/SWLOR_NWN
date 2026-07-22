namespace SWLOR.Game.Server.Service.EngineTestService
{
    /// <summary>
    /// Marks a public static method as an in-engine integration test.
    /// The method must accept a single EngineTestContext parameter and return either void or Task.
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
