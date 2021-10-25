using System.Collections.Generic;

namespace SWLOR.CLI.Model
{
    public class HakBuilderConfig
    {
        public HakBuilderConfig()
        {
            HakList = new List<HakBuilderHakpak>();
        }

        public string TlkPath { get; set; }
        public string OutputPath { get; set; }
        public List<HakBuilderHakpak> HakList { get; set; }
    }

    public class HakBuilderHakpak
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool CompileModels { get; set; }
    }
}
