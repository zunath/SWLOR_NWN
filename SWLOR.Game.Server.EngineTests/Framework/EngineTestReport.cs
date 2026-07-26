using System.Collections.Generic;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    public class EngineTestReport
    {
        public DateTime StartedUtc { get; set; }
        public DateTime FinishedUtc { get; set; }
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public List<EngineTestResult> Results { get; set; } = new();
    }
}
