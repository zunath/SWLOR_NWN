using System;
using System.Collections.Generic;
using System.IO;

namespace SWLOR.CLI
{
    internal class DeployBuild
    {
        private const string DebugServerPath = "../debugserver/";
        private const string DotnetPath = DebugServerPath + "dotnet";
        private const string HakPath = DebugServerPath + "hak";
        private const string ModulesPath = DebugServerPath + "modules";
        private const string TlkPath = DebugServerPath + "tlk";
        private const string DebugServerEnvPath = DebugServerPath + "swlor.env";
        private const string MaterialNameNullTweakVariable = "NWNX_TWEAKS_MATERIAL_NAME_NULL_IS_ALL";
        private const string MaterialNameNullTweakValue = "true";

        private readonly HakBuilder _hakBuilder = new();

        public void Process(string serverOutputPath = null)
        {
            // RunCLI builds Release for standalone invocations. Post-build deployment
            // explicitly passes the initiating build's TargetDir (including Debug/custom output).
            var source = new DirectoryInfo(string.IsNullOrWhiteSpace(serverOutputPath)
                ? "../SWLOR.Game.Server/bin/Release/net10.0/"
                : serverOutputPath);
            foreach (var filename in new[]
                     {
                         "SWLOR.Game.Server.dll", "SWLOR.Game.Server.deps.json",
                         "SWLOR.Game.Server.runtimeconfig.json"
                     })
            {
                if (!File.Exists(Path.Combine(source.FullName, filename)))
                    throw new FileNotFoundException($"Server build output is incomplete: {Path.Combine(source.FullName, filename)}");
            }

            CreateDebugServerDirectory();
            CopyBinaries(source);
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

            var source = new DirectoryInfo("../SWLOR.Game.Server/Docker");
            var target = new DirectoryInfo(DebugServerPath);

            CopyAll(source, target, "swlor.env");
            EnsureEnvironmentSetting(
                DebugServerEnvPath,
                MaterialNameNullTweakVariable,
                MaterialNameNullTweakValue);
        }

        private void CopyBinaries(DirectoryInfo source)
        {
            var target = new DirectoryInfo(DotnetPath);
            Console.WriteLine($"Deploying server binaries: {source.FullName} -> {target.FullName}");
            CopyAll(source, target, string.Empty);
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

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target, string excludeFile)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (var fi in source.GetFiles())
            {
                var targetPath = Path.Combine(target.FullName, fi.Name);

                if (File.Exists(targetPath) && fi.Name == excludeFile)
                    continue;

                fi.CopyTo(targetPath, true);
            }
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, excludeFile);
            }
        }

        private static void EnsureEnvironmentSetting(string path, string key, string value)
        {
            var prefix = key + "=";
            var updatedLines = new List<string>();
            var found = false;

            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    updatedLines.Add(line);
                    continue;
                }

                if (!found)
                {
                    updatedLines.Add(prefix + value);
                    found = true;
                }
            }

            if (!found)
                updatedLines.Add(prefix + value);

            File.WriteAllLines(path, updatedLines);
        }
    }
}
