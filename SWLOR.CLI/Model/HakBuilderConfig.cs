using System.Collections.Generic;

namespace SWLOR.CLI.Model
{
    public class HakBuilderConfig
    {
        public HakBuilderConfig()
        {
            HakList = new List<HakBuilderHakpak>();
            BeforeBuild = new List<HakBuilderBuildStep>();
            EnableChecksumChecking = true;
        }

        public string TlkPath { get; set; }
        public string OutputPath { get; set; }
        public List<HakBuilderHakpak> HakList { get; set; }
        public List<HakBuilderBuildStep> BeforeBuild { get; set; }
        public bool EnableChecksumChecking { get; set; }
    }

    public class HakBuilderHakpak
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool CompileModels { get; set; }
    }
}
