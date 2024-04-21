using System.IO;
using System.Linq;

namespace SWLOR.CLI
{
    internal class DeployBuild
    {
        private const string DebugServerPath = "../debugserver/";
        private const string DotnetPath = DebugServerPath + "dotnet";
        private const string PluginPath = DebugServerPath + "plugins";
        private const string HakPath = DebugServerPath + "hak";
        private const string ModulesPath = DebugServerPath + "modules";
        private const string TlkPath = DebugServerPath + "tlk";

        private readonly HakBuilder _hakBuilder = new();

        public void Process()
        {
            CreateDebugServerDirectory();
            CopyBinaries("../SWLOR.Core/bin/Debug/net7.0/");
            CopyPlugins();
            BuildHaks();
            BuildModule();
        }

        private void CreateDebugServerDirectory()
        {
            Directory.CreateDirectory(DebugServerPath);
            Directory.CreateDirectory(DotnetPath);
            Directory.CreateDirectory(HakPath);
            Directory.CreateDirectory(ModulesPath);
            Directory.CreateDirectory(TlkPath);

            var source = new DirectoryInfo("../SWLOR.Core/Docker");
            var target = new DirectoryInfo(DebugServerPath);

            CopyAll(source, target, "swlor.env");
        }

        private void CopyBinaries(string path)
        {
            var source = new DirectoryInfo(path);
            var target = new DirectoryInfo(DotnetPath);

            CopyAll(source, target, string.Empty);
        }

        private void CopyPlugins()
        {
            var pluginPath = "../plugins/Debug/net7.0/";

            var source = new DirectoryInfo(pluginPath);
            var target = new DirectoryInfo(PluginPath);
            
            CopyAll(source, target, "SWLOR.Core.dll", "SWLOR.Core.pdb");
        }
        
        private void BuildHaks()
        {
            _hakBuilder.Process();
        }

        private void BuildModule()
        {
            var modulePath = "../Module/Star Wars LOR v2.mod";
            File.Copy(modulePath, ModulesPath + "/Star Wars LOR v2.mod", true);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target, params string[] excludeFiles)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (var fi in source.GetFiles())
            {
                var targetPath = Path.Combine(target.FullName, fi.Name);

                if (File.Exists(targetPath) && excludeFiles.Contains(fi.Name))
                    continue;

                fi.CopyTo(targetPath, true);
            }
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, excludeFiles);
            }
        }
    }
}
