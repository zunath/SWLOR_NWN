using System;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.NWN.API.Service
{
    public static class ProfilerPlugin
    {
        private static IProfilerPluginService _service = new ProfilerPluginService();

        internal static void SetService(IProfilerPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IProfilerPluginService.PushPerfScope(string, string, string, string, string, string, string)"/>
        public static void PushPerfScope(
            string name, 
            string tag0_tag = "", 
            string tag0_value = "",
            string tag1_tag = "", 
            string tag1_value = "",
            string tag2_tag = "", 
            string tag2_value = "") => _service.PushPerfScope(name, tag0_tag, tag0_value, tag1_tag, tag1_value, tag2_tag, tag2_value);

        /// <inheritdoc cref="IProfilerPluginService.PushPerfScope(uint, string)"/>
        public static void PushPerfScope(uint target, string scriptName) => _service.PushPerfScope(target, scriptName);

        /// <inheritdoc cref="IProfilerPluginService.PopPerfScope"/>
        public static void PopPerfScope() => _service.PopPerfScope();
    }
}
