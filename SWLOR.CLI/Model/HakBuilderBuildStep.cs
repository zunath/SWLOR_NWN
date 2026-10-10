using System.Collections.Generic;

namespace SWLOR.CLI.Model
{
    public sealed class HakBuilderBuildStep
    {
        public string Command { get; set; }
        public List<string> Arguments { get; set; } = new();
        public List<string> OutputDirectories { get; set; } = new();
    }
}
