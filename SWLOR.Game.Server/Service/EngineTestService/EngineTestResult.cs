namespace SWLOR.Game.Server.Service.EngineTestService
{
    public class EngineTestResult
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public EngineTestOutcome Outcome { get; set; }
        public string Message { get; set; }
        public long DurationMilliseconds { get; set; }
    }
}
