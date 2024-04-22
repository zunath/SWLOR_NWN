using System.Reflection;

namespace SWLOR.Core.Plugin
{
    internal static class Assemblies
    {
        internal static readonly Assembly Swlor = typeof(Assemblies).Assembly;
        internal static readonly Assembly Native = typeof(NWN.Native.API.NWNXLib).Assembly;

        public static readonly Assembly[] AllAssemblies =
        {
            Swlor,
            Native,
            typeof(Newtonsoft.Json.JsonConvert).Assembly,
            typeof(global::Paket.Dependencies).Assembly,
        };

        public static readonly List<string> ReservedNames = AllAssemblies
            .Select(assembly => assembly.GetName().Name)
            .ToList()!;

        internal static readonly string AssemblyDir = Path.GetDirectoryName(Swlor.Location)!;

        internal static readonly string[] TargetFrameworks = { "net7.0" };

        public static bool IsReservedName(string name)
        {
            return ReservedNames.Contains(name);
        }
    }
}