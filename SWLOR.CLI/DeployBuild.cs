using System;
using System.IO;

namespace SWLOR.CLI
{
    internal class DeployBuild
    {
        private const string IniFileName = "./nwnpath.config";
        private const string DefaultNWNFolder = "%USERPROFILE%/Documents/Neverwinter Nights/";


        public void Process()
        {
            var nwnPath = File.Exists(IniFileName)
                ? File.ReadAllText(IniFileName)
                : DefaultNWNFolder;

            if (!Directory.Exists(nwnPath))
                nwnPath = DefaultNWNFolder;

            nwnPath = Environment.ExpandEnvironmentVariables(nwnPath + "dotnet");

            if (Directory.Exists(nwnPath))
                Directory.Delete(nwnPath, true);

            Directory.CreateDirectory(nwnPath);

            var binPath = "./bin/Debug/net6.0/";

            var source = new DirectoryInfo(binPath);
            var target = new DirectoryInfo(nwnPath);

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
