using System;
using System.IO;

namespace SWLOR.CLI
{
    internal class DeployBuild
    {
        private const string IniFileName = "./nwnpath.config";
        private const string DefaultDotnetFolder = "%USERPROFILE%/Documents/Neverwinter Nights/dotnet";


        public void Process()
        {
            var dotnetPath = File.Exists(IniFileName)
                ? File.ReadAllText(IniFileName)
                : DefaultDotnetFolder;

            if (!Directory.Exists(dotnetPath))
                dotnetPath = DefaultDotnetFolder;

            dotnetPath = Environment.ExpandEnvironmentVariables(dotnetPath);

            if (Directory.Exists(dotnetPath))
                Directory.Delete(dotnetPath, true);

            Directory.CreateDirectory(dotnetPath);

            var binPath = "./bin/Debug/net6.0/";

            var source = new DirectoryInfo(binPath);
            var target = new DirectoryInfo(dotnetPath);

            CopyAll(source, target);
        }


        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
